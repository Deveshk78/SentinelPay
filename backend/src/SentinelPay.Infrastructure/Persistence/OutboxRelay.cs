using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentinelPay.Application.Abstractions;
namespace SentinelPay.Infrastructure.Persistence;

/// <summary>Moves committed outbox rows to the bus. SKIP LOCKED lets several API instances relay safely. Delivery is at-least-once, so consumers must be idempotent.</summary>
public sealed class OutboxRelay(IServiceScopeFactory scopes, ILogger<OutboxRelay> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        while (await timer.WaitForNextTickAsync(ct))
            try { await DrainAsync(ct); } catch (OperationCanceledException) { } catch (Exception ex) { log.LogError(ex, "Outbox relay iteration failed"); }
    }

    internal async Task DrainAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        await using var trx = await db.Database.BeginTransactionAsync(ct);
        var batch = await db.Outbox.FromSqlRaw(
            "SELECT * FROM outbox_messages WHERE \"ProcessedAt\" IS NULL AND \"Attempts\" < 10 ORDER BY \"CreatedAt\" LIMIT 50 FOR UPDATE SKIP LOCKED").ToListAsync(ct);
        foreach (var m in batch)
        {
            try
            {
                using var doc = JsonDocument.Parse(m.Payload);
                var corr = doc.RootElement.TryGetProperty("CorrelationId", out var c) ? c.GetString() ?? "" : "";
                await publisher.PublishAsync(m.Type, m.Payload, corr, ct);
                m.MarkProcessed();
            }
            catch (Exception ex) { m.MarkFailed(ex.Message); log.LogWarning(ex, "Outbox publish failed for {Id}, attempt {N}", m.Id, m.Attempts); }
        }
        await db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);
    }
}
