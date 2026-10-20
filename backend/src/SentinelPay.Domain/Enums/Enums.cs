namespace SentinelPay.Domain.Enums;

public enum AccountStatus { Active, Frozen, Closed }
public enum TransactionStatus { PendingRisk, RiskReview, Approved, Declined, Routed, Settled, Failed }
public enum RiskDecision { Approve, Review, Decline }
/// <summary>None = unscored, SyncFast = in-request check, Deep = agent graph after the outbox.</summary>
public enum RiskStage { None, SyncFast, Deep }
