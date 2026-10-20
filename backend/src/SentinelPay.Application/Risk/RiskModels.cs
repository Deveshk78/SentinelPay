using SentinelPay.Domain.Enums;
namespace SentinelPay.Application.Risk;

/// <summary>Features only. No names, emails, card numbers or free text: safe to send to a model.</summary>
public sealed record RiskContext(Guid TransactionId, Guid AccountId, decimal Amount, string Currency, string MerchantId, string CorrelationId);
public sealed record RiskAssessment(int Score, string Agent, string ModelId, string Rationale, string InputHash, long LatencyMs);

public static class RiskPolicy
{
    public static RiskDecision Decide(int score, RiskOptions o) =>
        score >= o.DeclineThreshold ? RiskDecision.Decline : score >= o.ReviewThreshold ? RiskDecision.Review : RiskDecision.Approve;
}
