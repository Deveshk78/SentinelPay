using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Risk;
using SentinelPay.Domain.Enums;
using StackExchange.Redis;
namespace SentinelPay.Infrastructure.Risk;

/// <summary>Deterministic fast path (a few ms): amount tiers plus a per-account 60 s velocity counter in Redis.</summary>
public sealed class RuleBasedRiskScorer(IConnectionMultiplexer redis)
{
    public async Task<RiskAssessment> ScoreAsync(RiskContext c, string note = "")
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var db = redis.GetDatabase();
        var key = $"vel:{c.AccountId}";
        var velocity = await db.StringIncrementAsync(key);
        if (velocity == 1) await db.KeyExpireAsync(key, TimeSpan.FromSeconds(60));
        var score = 0; var reasons = new List<string>();
        if (c.Amount >= 50_000) { score += 40; reasons.Add("very high amount"); } else if (c.Amount >= 10_000) { score += 20; reasons.Add("high amount"); } else if (c.Amount >= 1_000) score += 5;
        if (velocity > 5) { score += 45; reasons.Add($"{velocity} payments in 60s"); } else if (velocity > 3) { score += 20; reasons.Add($"{velocity} payments in 60s"); }
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{c.Amount}|{c.Currency}|{c.MerchantId}|{velocity}")));
        return new(Math.Min(score, 100), "RuleScorer", "rules-v1", $"{note}{(reasons.Count == 0 ? "No risk signals." : string.Join("; ", reasons))}", hash, sw.ElapsedMilliseconds);
    }
}

/// <summary>SyncFast uses rules unless Risk:SyncUsesAgent is on. Deep uses the agent graph and falls back to rules if the model is down.</summary>
public sealed class RiskScoringRouter(RuleBasedRiskScorer rules, IEnumerable<IAgentRiskGraph> graphs,
    Microsoft.Extensions.Options.IOptionsMonitor<RiskOptions> options, ILogger<RiskScoringRouter> log) : IRiskScoringService
{
    public async Task<RiskAssessment> ScoreAsync(RiskContext ctx, RiskStage stage, CancellationToken ct)
    {
        var graph = graphs.FirstOrDefault();
        var useAgent = graph is not null && (stage == RiskStage.Deep || options.CurrentValue.SyncUsesAgent);
        if (!useAgent) return await rules.ScoreAsync(ctx);
        try { return await graph!.RunAsync(ctx, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Agent graph failed for {Tx}; using rules", ctx.TransactionId);
            return await rules.ScoreAsync(ctx, "Agent fallback: ");
        }
    }
}
