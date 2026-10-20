namespace SentinelPay.Application.Risk;
/// <summary>Port implemented by SentinelPay.Agents (multi-agent graph). Kept out of Infrastructure so the agent package is swappable.</summary>
public interface IAgentRiskGraph { Task<RiskAssessment> RunAsync(RiskContext ctx, CancellationToken ct); }
