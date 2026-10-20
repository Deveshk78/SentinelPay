using FluentAssertions;
using SentinelPay.Domain.Common;
using SentinelPay.Domain.Entities;
using SentinelPay.Domain.Enums;
using Xunit;
namespace SentinelPay.Domain.Tests;

public class TransactionTests
{
    private static Transaction New(decimal amt = 100) => Transaction.Create(Guid.NewGuid(), amt, "inr", "shop-1", "idem-12345", "corr-1");

    [Fact] public void New_transaction_waits_for_risk() => New().Status.Should().Be(TransactionStatus.PendingRisk);
    [Fact] public void Zero_amount_is_rejected() => FluentActions.Invoking(() => New(0)).Should().Throw<DomainException>();

    [Theory]
    [InlineData(RiskDecision.Approve, TransactionStatus.Approved)]
    [InlineData(RiskDecision.Review, TransactionStatus.RiskReview)]
    [InlineData(RiskDecision.Decline, TransactionStatus.Declined)]
    public void Risk_decision_sets_status(RiskDecision d, TransactionStatus expected)
    { var t = New(); t.ApplyRisk(10, d, RiskStage.SyncFast); t.Status.Should().Be(expected); }

    [Fact] public void Cannot_route_unless_approved() => FluentActions.Invoking(() => New().MarkRouted("Sandbox")).Should().Throw<DomainException>();

    [Fact] public void Risk_cannot_change_after_routing()
    {
        var t = New(); t.ApplyRisk(5, RiskDecision.Approve, RiskStage.Deep); t.MarkRouted("Sandbox");
        FluentActions.Invoking(() => t.ApplyRisk(90, RiskDecision.Decline, RiskStage.Deep)).Should().Throw<DomainException>();
    }
    [Fact] public void Happy_path_reaches_settled()
    { var t = New(); t.ApplyRisk(5, RiskDecision.Approve, RiskStage.Deep); t.MarkRouted("Sandbox"); t.MarkSettled(); t.Status.Should().Be(TransactionStatus.Settled); }
}

public class AccountTests
{
    [Fact] public void Debit_reduces_balance() { var a = Account.Open(Guid.NewGuid(), "INR", 500); a.Debit(200); a.Balance.Should().Be(300); }
    [Fact] public void Overdraft_is_rejected() => FluentActions.Invoking(() => Account.Open(Guid.NewGuid(), "INR", 50).Debit(51)).Should().Throw<DomainException>();
    [Fact] public void Frozen_account_cannot_afford() { var a = Account.Open(Guid.NewGuid(), "INR", 500); a.Freeze(); a.CanAfford(1, "INR").Should().BeFalse(); }
}
