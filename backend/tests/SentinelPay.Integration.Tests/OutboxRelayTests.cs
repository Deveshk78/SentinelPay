using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SentinelPay.Application.Abstractions;
using SentinelPay.Domain.Entities;
using SentinelPay.Domain.Events;
using SentinelPay.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;
namespace SentinelPay.Integration.Tests;

public sealed class RecordingPublisher : IEventPublisher
{
    public ConcurrentBag<string> Published { get; } = [];
    public volatile bool Fail; public int DelayMs;
    public async Task PublishAsync(string type, string payload, string correlationId, CancellationToken ct)
    {
        if (DelayMs > 0) await Task.Delay(DelayMs, ct);
        if (Fail) throw new InvalidOperationException("broker down");
        Published.Add(payload);
    }
}

/// <summary>Real PostgreSQL: proves the relay's SKIP LOCKED query, retry accounting and no-duplicate behaviour.</summary>
public class OutboxRelayTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
    private readonly RecordingPublisher _pub = new();
    private ServiceProvider _sp = null!; private OutboxRelay _relay = null!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        var s = new ServiceCollection();
        s.AddDbContext<AppDbContext>(o => o.UseNpgsql(_pg.GetConnectionString())); s.AddSingleton<IEventPublisher>(_pub);
        _sp = s.BuildServiceProvider();
        using (var scope = _sp.CreateScope()) await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        _relay = new OutboxRelay(_sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxRelay>.Instance);
    }
    public async Task DisposeAsync() { await _sp.DisposeAsync(); await _pg.DisposeAsync(); }

    private async Task Seed(int n)
    {
        using var scope = _sp.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        for (var i = 0; i < n; i++)
            db.Outbox.Add(OutboxMessage.Create(nameof(PaymentRequested), JsonSerializer.Serialize(new PaymentRequested(Guid.NewGuid(), "corr-12345678", false))));
        await db.SaveChangesAsync();
    }
    private async Task<List<OutboxMessage>> Rows() { using var scope = _sp.CreateScope(); return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Outbox.AsNoTracking().ToListAsync(); }

    [Fact] public async Task Each_committed_row_is_published_once_and_marked_processed()
    {
        await Seed(3);
        await _relay.DrainAsync(default); await _relay.DrainAsync(default);
        _pub.Published.Should().HaveCount(3); (await Rows()).Should().OnlyContain(r => r.ProcessedAt != null);
    }
    [Fact] public async Task Broker_outage_keeps_rows_and_counts_attempts_then_recovers()
    {
        await Seed(2); _pub.Fail = true;
        await _relay.DrainAsync(default);
        var during = await Rows(); during.Should().OnlyContain(r => r.ProcessedAt == null && r.Attempts == 1 && r.Error != null);
        _pub.Fail = false; await _relay.DrainAsync(default);
        _pub.Published.Should().HaveCount(2); (await Rows()).Should().OnlyContain(r => r.ProcessedAt != null);
    }
    [Fact] public async Task Two_relays_running_at_once_never_publish_the_same_row_twice()
    {
        await Seed(20); _pub.DelayMs = 50;
        await Task.WhenAll(_relay.DrainAsync(default), _relay.DrainAsync(default));
        _pub.Published.Should().HaveCount(20); _pub.Published.Distinct().Should().HaveCount(20);
    }
    [Fact] public async Task Poison_rows_stop_being_retried_after_ten_attempts()
    {
        await Seed(1); _pub.Fail = true;
        for (var i = 0; i < 12; i++) await _relay.DrainAsync(default);
        (await Rows()).Single().Attempts.Should().Be(10);
    }
}
