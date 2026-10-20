using SentinelPay.Domain.Common;
using SentinelPay.Domain.Enums;
namespace SentinelPay.Domain.Entities;

public sealed class Transaction : Entity
{
    private Transaction() { }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public string MerchantId { get; private set; } = "";
    public TransactionStatus Status { get; private set; } = TransactionStatus.PendingRisk;
    public int? RiskScore { get; private set; }
    public RiskDecision? RiskDecision { get; private set; }
    public RiskStage RiskStage { get; private set; } = RiskStage.None;
    public string? Gateway { get; private set; }
    public string IdempotencyKey { get; private set; } = "";
    public string CorrelationId { get; private set; } = "";

    public static Transaction Create(Guid accountId, decimal amount, string currency, string merchantId, string idempotencyKey, string correlationId)
    {
        if (amount <= 0) throw new DomainException("Amount must be positive.");
        return new() { AccountId = accountId, Amount = amount, Currency = currency.ToUpperInvariant(), MerchantId = merchantId,
                       IdempotencyKey = idempotencyKey, CorrelationId = correlationId };
    }

    public void ApplyRisk(int score, RiskDecision decision, RiskStage stage)
    {
        if (Status is TransactionStatus.Routed or TransactionStatus.Settled)
            throw new DomainException("Risk cannot change after routing.");
        RiskScore = score; RiskDecision = decision; RiskStage = stage;
        Status = decision switch { Enums.RiskDecision.Approve => TransactionStatus.Approved,
                                   Enums.RiskDecision.Review => TransactionStatus.RiskReview, _ => TransactionStatus.Declined };
        Touch();
    }
    public void MarkRouted(string gateway)
    {
        if (Status != TransactionStatus.Approved) throw new DomainException($"Cannot route from {Status}.");
        Gateway = gateway; Status = TransactionStatus.Routed; Touch();
    }
    public void MarkSettled() { if (Status != TransactionStatus.Routed) throw new DomainException($"Cannot settle from {Status}."); Status = TransactionStatus.Settled; Touch(); }
    public void MarkFailed() { Status = TransactionStatus.Failed; Touch(); }
}
