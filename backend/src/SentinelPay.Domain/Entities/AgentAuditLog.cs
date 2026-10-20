using SentinelPay.Domain.Common;
using SentinelPay.Domain.Enums;
namespace SentinelPay.Domain.Entities;

/// <summary>Immutable record of one agent decision. Stores hashes and reasons, never raw PII.</summary>
public sealed class AgentAuditLog : Entity
{
    private AgentAuditLog() { }
    public string CorrelationId { get; private set; } = "";
    public Guid TransactionId { get; private set; }
    public string AgentName { get; private set; } = "";
    public RiskStage Stage { get; private set; }
    public string InputHash { get; private set; } = "";   // SHA-256 of the features given to the agent
    public RiskDecision Decision { get; private set; }
    public int Score { get; private set; }
    public string Rationale { get; private set; } = "";
    public string ModelId { get; private set; } = "";      // "rules-v1" or the LLM deployment name
    public long LatencyMs { get; private set; }
    public bool TimedOut { get; private set; }

    public static AgentAuditLog Record(string correlationId, Guid txId, string agent, RiskStage stage, string inputHash,
        RiskDecision decision, int score, string rationale, string modelId, long latencyMs, bool timedOut = false) =>
        new() { CorrelationId = correlationId, TransactionId = txId, AgentName = agent, Stage = stage, InputHash = inputHash,
                Decision = decision, Score = score, Rationale = rationale, ModelId = modelId, LatencyMs = latencyMs, TimedOut = timedOut };
}
