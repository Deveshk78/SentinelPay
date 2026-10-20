namespace SentinelPay.Application.Risk;

/// <summary>Off = no scoring. Sync = score in-request, decision is final. Async = score after the outbox.
/// Hybrid = fast in-request check; clear declines stop immediately, low-risk skips the deep stage, the rest go to the agent graph.</summary>
public enum RiskMode { Off, Sync, Async, Hybrid }
public enum TimeoutAction { DeferToDeep, Decline, Approve }

public sealed class RiskOptions
{
    public const string Section = "Risk";
    public RiskMode Mode { get; set; } = RiskMode.Hybrid;
    public int SyncTimeoutMs { get; set; } = 400;
    public TimeoutAction OnSyncTimeout { get; set; } = TimeoutAction.DeferToDeep;
    public int ReviewThreshold { get; set; } = 50;
    public int DeclineThreshold { get; set; } = 80;
    public int SkipDeepBelowScore { get; set; } = 20;
    public decimal AlwaysDeepAbove { get; set; } = 50_000m;
    public bool SyncUsesAgent { get; set; } = false;   // false = deterministic rules (<5 ms); true = agent graph inside the timeout
}
