using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Risk;
using SentinelPay.Domain.Entities;
using SentinelPay.Domain.Enums;
using SentinelPay.Domain.Events;
namespace SentinelPay.Application.Payments;

public sealed record CreatePaymentCommand(Guid AccountId, Guid CallerOwnerId, decimal Amount, string Currency, string MerchantId, string IdempotencyKey, string CorrelationId);
public sealed record PaymentResult(Guid? TransactionId, string Status, int? RiskScore, string? Message);

public sealed class CreatePaymentHandler(IAppDbContext db, IRiskScoringService risk, IIdempotencyStore idem,
    IOptionsMonitor<RiskOptions> options, ILogger<CreatePaymentHandler> log)
{
    public async Task<PaymentResult> HandleAsync(CreatePaymentCommand c, CancellationToken ct)
    {
        var opt = options.CurrentValue;

        // 1. Idempotency: Redis lock for concurrent duplicates, unique DB index as the final guard.
        var key = $"{c.AccountId}:{c.IdempotencyKey}";
        var existing = await db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.AccountId == c.AccountId && t.IdempotencyKey == c.IdempotencyKey, ct);
        if (existing is not null) return new(existing.Id, existing.Status.ToString(), existing.RiskScore, "Duplicate request: original result returned.");
        if (!await idem.TryAcquireAsync(key, TimeSpan.FromMinutes(10), ct))
            return new(null, "Conflict", null, "A request with this Idempotency-Key is already in progress.");

        // 2. Ownership check (OWASP A01: broken object-level authorisation) and funds check.
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == c.AccountId && a.OwnerId == c.CallerOwnerId, ct);
        if (account is null) return new(null, "NotFound", null, "Account not found.");
        if (!account.CanAfford(c.Amount, c.Currency)) return new(null, "Rejected", null, "Insufficient funds or inactive account.");

        var tx = Transaction.Create(c.AccountId, c.Amount, c.Currency, c.MerchantId, c.IdempotencyKey, c.CorrelationId);
        var ctx = new RiskContext(tx.Id, c.AccountId, c.Amount, tx.Currency, c.MerchantId, c.CorrelationId);
        var deepRequired = false;

        // 3. Configurable risk stage.
        switch (opt.Mode)
        {
            case RiskMode.Off:
                tx.ApplyRisk(0, RiskDecision.Approve, RiskStage.None);
                break;
            case RiskMode.Async:
                deepRequired = true;
                break;
            case RiskMode.Sync or RiskMode.Hybrid:
                var (assessment, timedOut) = await TryScoreFastAsync(ctx, opt, ct);
                if (timedOut)
                {
                    log.LogWarning("Sync risk timed out after {Ms} ms; action {Action}", opt.SyncTimeoutMs, opt.OnSyncTimeout);
                    db.AgentAuditLogs.Add(AgentAuditLog.Record(c.CorrelationId, tx.Id, "FastRisk", RiskStage.SyncFast, "n/a",
                        RiskDecision.Review, 0, "Timed out", "n/a", opt.SyncTimeoutMs, timedOut: true));
                    switch (opt.OnSyncTimeout)
                    {
                        case TimeoutAction.Decline: tx.ApplyRisk(100, RiskDecision.Decline, RiskStage.SyncFast); break;
                        case TimeoutAction.Approve: tx.ApplyRisk(0, RiskDecision.Approve, RiskStage.SyncFast); break;
                        default:
                            if (opt.Mode == RiskMode.Hybrid) deepRequired = true;
                            else tx.ApplyRisk(opt.ReviewThreshold, RiskDecision.Review, RiskStage.SyncFast);  // Sync mode has no deep stage
                            break;
                    }
                    break;
                }
                var decision = RiskPolicy.Decide(assessment!.Score, opt);
                db.AgentAuditLogs.Add(AgentAuditLog.Record(c.CorrelationId, tx.Id, assessment.Agent, RiskStage.SyncFast, assessment.InputHash,
                    decision, assessment.Score, assessment.Rationale, assessment.ModelId, assessment.LatencyMs));
                var lowRisk = decision == RiskDecision.Approve && assessment.Score < opt.SkipDeepBelowScore && c.Amount <= opt.AlwaysDeepAbove;
                if (opt.Mode == RiskMode.Sync || decision == RiskDecision.Decline || lowRisk)
                    tx.ApplyRisk(assessment.Score, decision, RiskStage.SyncFast);
                else deepRequired = true;   // Hybrid: undecided cases wait for the agent graph
                break;
        }

        db.Transactions.Add(tx);

        // 4. One commit: the transaction and its outbox event. No dual-write.
        IIntegrationEvent evt = tx.Status == TransactionStatus.Declined
            ? new PaymentDeclined(tx.Id, c.CorrelationId, "Declined by fast risk check")
            : new PaymentRequested(tx.Id, c.CorrelationId, deepRequired);
        var (type, payload) = EventSerializer.Serialize(evt);
        db.Outbox.Add(OutboxMessage.Create(type, payload));
        await db.SaveChangesAsync(ct);

        var message = tx.Status switch
        {
            TransactionStatus.Declined => "Payment declined.",
            TransactionStatus.RiskReview => "Payment held for review.",
            TransactionStatus.PendingRisk => "Payment accepted; risk checks in progress.",
            _ => "Payment accepted."
        };
        return new(tx.Id, tx.Status.ToString(), tx.RiskScore, message);
    }

    private async Task<(RiskAssessment? Result, bool TimedOut)> TryScoreFastAsync(RiskContext ctx, RiskOptions opt, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(opt.SyncTimeoutMs);
        try { return (await risk.ScoreAsync(ctx, RiskStage.SyncFast, cts.Token), false); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return (null, true); }
    }
}
