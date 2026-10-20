using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Payments;
using SentinelPay.Application.Risk;
using SentinelPay.Domain.Entities;
using SentinelPay.Domain.Enums;
using SentinelPay.Domain.Events;
using Xunit;
namespace SentinelPay.Application.Tests;

public sealed class TestDb(DbContextOptions<TestDb> o) : DbContext(o), IAppDbContext
{
    public DbSet<Account> Accounts => Set<Account>(); public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AgentAuditLog> AgentAuditLogs => Set<AgentAuditLog>(); public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
}

public class CreatePaymentHandlerTests
{
    private readonly Guid _owner = Guid.NewGuid();
    private readonly TestDb _db = new(new DbContextOptionsBuilder<TestDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly IIdempotencyStore _idem = Substitute.For<IIdempotencyStore>();
    private readonly IRiskScoringService _risk = Substitute.For<IRiskScoringService>();
    private Account _acct = null!;

    private CreatePaymentHandler Sut(RiskOptions o)
    {
        _idem.TryAcquireAsync(default!, default, default).ReturnsForAnyArgs(true);
        _acct = Account.Open(_owner, "INR", 1_000_000); _db.Accounts.Add(_acct); _db.SaveChanges();
        var opts = Substitute.For<IOptionsMonitor<RiskOptions>>(); opts.CurrentValue.Returns(o);
        return new(_db, _risk, _idem, opts, NullLogger<CreatePaymentHandler>.Instance);
    }
    private void Score(int score) => _risk.ScoreAsync(default!, default, default).ReturnsForAnyArgs(new RiskAssessment(score, "Rules", "rules-v1", "test", "hash", 1));
    private CreatePaymentCommand Cmd(decimal amt = 500, string key = "idem-0001") => new(_acct.Id, _owner, amt, "INR", "shop-1", key, "corr-12345678");
    private PaymentRequested? LastRequested() => _db.Outbox.AsEnumerable().Where(m => m.Type == nameof(PaymentRequested)).Select(m => (PaymentRequested)EventSerializer.Deserialize(m.Type, m.Payload)!).FirstOrDefault();

    [Fact] public async Task Hybrid_high_score_declines_immediately()
    {
        var sut = Sut(new() { Mode = RiskMode.Hybrid }); Score(90);
        var r = await sut.HandleAsync(Cmd(), default);
        r.Status.Should().Be("Declined"); _db.Outbox.Single().Type.Should().Be(nameof(PaymentDeclined));
    }
    [Fact] public async Task Hybrid_low_score_skips_deep_stage()
    {
        var sut = Sut(new() { Mode = RiskMode.Hybrid }); Score(5);
        var r = await sut.HandleAsync(Cmd(), default);
        r.Status.Should().Be("Approved"); LastRequested()!.DeepRiskRequired.Should().BeFalse();
    }
    [Fact] public async Task Hybrid_mid_score_defers_to_deep()
    {
        var sut = Sut(new() { Mode = RiskMode.Hybrid }); Score(35);
        var r = await sut.HandleAsync(Cmd(), default);
        r.Status.Should().Be("PendingRisk"); LastRequested()!.DeepRiskRequired.Should().BeTrue();
    }
    [Fact] public async Task Hybrid_large_amount_always_goes_deep()
    {
        var sut = Sut(new() { Mode = RiskMode.Hybrid, AlwaysDeepAbove = 1000 }); Score(1);
        (await sut.HandleAsync(Cmd(5000), default)).Status.Should().Be("PendingRisk");
    }
    [Fact] public async Task Sync_timeout_follows_configured_action()
    {
        var sut = Sut(new() { Mode = RiskMode.Hybrid, SyncTimeoutMs = 30, OnSyncTimeout = TimeoutAction.DeferToDeep });
        _risk.ScoreAsync(default!, default, default).ReturnsForAnyArgs(async ci => { await Task.Delay(2000, ci.ArgAt<CancellationToken>(2)); return null!; });
        var r = await sut.HandleAsync(Cmd(), default);
        r.Status.Should().Be("PendingRisk"); _db.AgentAuditLogs.Single().TimedOut.Should().BeTrue();
    }
    [Fact] public async Task Sync_timeout_can_fail_closed()
    {
        var sut = Sut(new() { Mode = RiskMode.Sync, SyncTimeoutMs = 30, OnSyncTimeout = TimeoutAction.Decline });
        _risk.ScoreAsync(default!, default, default).ReturnsForAnyArgs(async ci => { await Task.Delay(2000, ci.ArgAt<CancellationToken>(2)); return null!; });
        (await sut.HandleAsync(Cmd(), default)).Status.Should().Be("Declined");
    }
    [Fact] public async Task Async_mode_never_scores_in_request()
    {
        var sut = Sut(new() { Mode = RiskMode.Async });
        var r = await sut.HandleAsync(Cmd(), default);
        r.Status.Should().Be("PendingRisk"); await _risk.DidNotReceiveWithAnyArgs().ScoreAsync(default!, default, default);
    }
    [Fact] public async Task Off_mode_approves_without_scoring()
    {
        var sut = Sut(new() { Mode = RiskMode.Off });
        (await sut.HandleAsync(Cmd(), default)).Status.Should().Be("Approved");
    }
    [Fact] public async Task Duplicate_key_returns_original_without_second_row()
    {
        var sut = Sut(new() { Mode = RiskMode.Off });
        var first = await sut.HandleAsync(Cmd(), default); var second = await sut.HandleAsync(Cmd(), default);
        second.TransactionId.Should().Be(first.TransactionId); _db.Transactions.Count().Should().Be(1);
    }
    [Fact] public async Task Other_users_account_is_not_found()
    {
        var sut = Sut(new() { Mode = RiskMode.Off });
        (await sut.HandleAsync(Cmd() with { CallerOwnerId = Guid.NewGuid() }, default)).Status.Should().Be("NotFound");
    }
}
