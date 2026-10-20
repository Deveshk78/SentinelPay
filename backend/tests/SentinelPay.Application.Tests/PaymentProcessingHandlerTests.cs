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

public class PaymentProcessingHandlerTests
{
    private readonly TestDb _db = new(new DbContextOptionsBuilder<TestDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly IRiskScoringService _risk = Substitute.For<IRiskScoringService>();
    private readonly IPaymentGatewayRouter _router = Substitute.For<IPaymentGatewayRouter>();
    private readonly IGatewayClient _gw = Substitute.For<IGatewayClient>();
    private Account _acct = null!;

    private PaymentProcessingHandler Sut(decimal balance = 10_000, bool gatewayOk = true)
    {
        _acct = Account.Open(Guid.NewGuid(), "INR", balance); _db.Accounts.Add(_acct); _db.SaveChanges();
        _router.SelectAsync(default!, default).ReturnsForAnyArgs(new GatewayChoice("Sandbox", "cheapest"));
        _gw.ChargeAsync(default!, default!, default).ReturnsForAnyArgs(gatewayOk);
        var o = Substitute.For<IOptionsMonitor<RiskOptions>>(); o.CurrentValue.Returns(new RiskOptions());
        return new(_db, _risk, _router, _gw, o, NullLogger<PaymentProcessingHandler>.Instance);
    }
    private Transaction Tx(decimal amount = 500, RiskDecision? decision = RiskDecision.Approve)
    {
        var t = Transaction.Create(_acct.Id, amount, "INR", "shop-1", Guid.NewGuid().ToString(), "corr-12345678");
        if (decision is { } d) t.ApplyRisk(5, d, RiskStage.SyncFast);
        _db.Transactions.Add(t); _db.SaveChanges(); return t;
    }
    private static PaymentRequested Evt(Transaction t, bool deep = false) => new(t.Id, t.CorrelationId, deep);
    private void Score(int score) => _risk.ScoreAsync(default!, default, default).ReturnsForAnyArgs(new RiskAssessment(score, "RiskGraph", "model-x", "test", "hash", 5));
    private List<string> OutboxTypes() => _db.Outbox.Select(m => m.Type).ToList();

    [Fact] public async Task Approved_payment_is_reserved_charged_and_settled()
    {
        var sut = Sut(); var t = Tx(500);
        await sut.HandleAsync(Evt(t), default);
        t.Status.Should().Be(TransactionStatus.Settled); _acct.Balance.Should().Be(9_500);
        OutboxTypes().Should().Equal(nameof(PaymentRouted), nameof(PaymentSettled));
    }
    [Fact] public async Task Redelivered_event_never_charges_twice()
    {
        var sut = Sut(); var t = Tx(500);
        await sut.HandleAsync(Evt(t), default); await sut.HandleAsync(Evt(t), default); await sut.HandleAsync(Evt(t), default);
        await _gw.ReceivedWithAnyArgs(1).ChargeAsync(default!, default!, default);
        _acct.Balance.Should().Be(9_500);
    }
    [Fact] public async Task Declined_gateway_charge_releases_the_reserved_funds()
    {
        var sut = Sut(gatewayOk: false); var t = Tx(500);
        await sut.HandleAsync(Evt(t), default);
        t.Status.Should().Be(TransactionStatus.Failed); _acct.Balance.Should().Be(10_000);
        OutboxTypes().Should().NotContain(nameof(PaymentSettled));
    }
    [Fact] public async Task Insufficient_funds_fails_without_ever_calling_the_gateway()
    {
        var sut = Sut(balance: 100); var t = Tx(500);
        await sut.HandleAsync(Evt(t), default);
        t.Status.Should().Be(TransactionStatus.Failed); _acct.Balance.Should().Be(100);
        await _gw.DidNotReceiveWithAnyArgs().ChargeAsync(default!, default!, default);
    }
    [Theory]
    [InlineData(RiskDecision.Decline)]
    [InlineData(RiskDecision.Review)]
    public async Task Declined_or_held_payments_are_never_charged(RiskDecision d)
    {
        var sut = Sut(); var t = Tx(500, d);
        await sut.HandleAsync(Evt(t), default);
        await _gw.DidNotReceiveWithAnyArgs().ChargeAsync(default!, default!, default); _acct.Balance.Should().Be(10_000);
    }
    [Fact] public async Task Deep_stage_decline_stops_everything_and_is_audited()
    {
        var sut = Sut(); var t = Tx(500, decision: null); Score(95);
        await sut.HandleAsync(Evt(t, deep: true), default);
        t.Status.Should().Be(TransactionStatus.Declined); _acct.Balance.Should().Be(10_000);
        OutboxTypes().Should().Equal(nameof(PaymentDeclined));
        var audit = _db.AgentAuditLogs.Single(); audit.Stage.Should().Be(RiskStage.Deep); audit.Score.Should().Be(95);
        await _gw.DidNotReceiveWithAnyArgs().ChargeAsync(default!, default!, default);
    }
    [Fact] public async Task Deep_stage_review_holds_the_payment()
    {
        var sut = Sut(); var t = Tx(500, decision: null); Score(60);
        await sut.HandleAsync(Evt(t, deep: true), default);
        t.Status.Should().Be(TransactionStatus.RiskReview); _acct.Balance.Should().Be(10_000);
        await _gw.DidNotReceiveWithAnyArgs().ChargeAsync(default!, default!, default);
    }
    [Fact] public async Task Deep_stage_approval_continues_to_settlement()
    {
        var sut = Sut(); var t = Tx(500, decision: null); Score(10);
        await sut.HandleAsync(Evt(t, deep: true), default);
        t.Status.Should().Be(TransactionStatus.Settled); t.RiskStage.Should().Be(RiskStage.Deep);
    }
    [Fact] public async Task Deep_stage_is_not_repeated_on_redelivery_after_a_crash()
    {
        var sut = Sut(); var t = Tx(500, decision: null); t.ApplyRisk(10, RiskDecision.Approve, RiskStage.Deep); _db.SaveChanges();
        await sut.HandleAsync(Evt(t, deep: true), default);
        await _risk.DidNotReceiveWithAnyArgs().ScoreAsync(default!, default, default);
        t.Status.Should().Be(TransactionStatus.Settled);
    }
    [Fact] public async Task Unknown_transaction_is_ignored_not_thrown()
    {
        var sut = Sut();
        await sut.Invoking(s => s.HandleAsync(new PaymentRequested(Guid.NewGuid(), "corr-12345678", false), default)).Should().NotThrowAsync();
    }
}
