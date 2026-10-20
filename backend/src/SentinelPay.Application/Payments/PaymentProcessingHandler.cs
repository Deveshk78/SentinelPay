using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Risk;
using SentinelPay.Domain.Common;
using SentinelPay.Domain.Entities;
using SentinelPay.Domain.Enums;
using SentinelPay.Domain.Events;
namespace SentinelPay.Application.Payments;

/// <summary>Consumer side: runs deep risk (if requested), routes, reserves funds, charges, settles. Safe to redeliver: every step checks current status.</summary>
public sealed class PaymentProcessingHandler(IAppDbContext db, IRiskScoringService risk, IPaymentGatewayRouter router,
    IGatewayClient gateways, IOptionsMonitor<RiskOptions> options, ILogger<PaymentProcessingHandler> log)
{
    public async Task HandleAsync(PaymentRequested e, CancellationToken ct)
    {
        var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == e.TransactionId, ct);
        if (tx is null) { log.LogWarning("Transaction {Id} not found", e.TransactionId); return; }
        if (tx.Status is TransactionStatus.Declined or TransactionStatus.Failed or TransactionStatus.RiskReview
            or TransactionStatus.Routed or TransactionStatus.Settled) return;   // already handled: idempotent

        if (e.DeepRiskRequired && tx.RiskStage != RiskStage.Deep)
        {
            var a = await risk.ScoreAsync(new RiskContext(tx.Id, tx.AccountId, tx.Amount, tx.Currency, tx.MerchantId, tx.CorrelationId), RiskStage.Deep, ct);
            var decision = RiskPolicy.Decide(a.Score, options.CurrentValue);
            db.AgentAuditLogs.Add(AgentAuditLog.Record(tx.CorrelationId, tx.Id, a.Agent, RiskStage.Deep, a.InputHash, decision, a.Score, a.Rationale, a.ModelId, a.LatencyMs));
            tx.ApplyRisk(a.Score, decision, RiskStage.Deep);
            if (decision == RiskDecision.Decline)
            {
                var (t, p) = EventSerializer.Serialize(new PaymentDeclined(tx.Id, tx.CorrelationId, "Declined by deep risk analysis"));
                db.Outbox.Add(OutboxMessage.Create(t, p));
            }
            await db.SaveChangesAsync(ct);
            if (decision != RiskDecision.Approve) return;
        }

        var choice = await router.SelectAsync(tx, ct);
        var account = await db.Accounts.FirstAsync(a => a.Id == tx.AccountId, ct);

        // Reserve (debit) BEFORE charging, so a failed debit can never follow a successful charge.
        try { account.Debit(tx.Amount); }
        catch (DomainException ex)
        {
            log.LogWarning(ex, "Could not reserve funds for {Id}", tx.Id);
            tx.MarkFailed();
            await db.SaveChangesAsync(ct);
            return;
        }
        tx.MarkRouted(choice.Name);
        var (rt, rp) = EventSerializer.Serialize(new PaymentRouted(tx.Id, tx.CorrelationId, choice.Name));
        db.Outbox.Add(OutboxMessage.Create(rt, rp));
        await db.SaveChangesAsync(ct);   // routed + funds reserved in one commit; xmin on Account turns a race into a retry

        if (await gateways.ChargeAsync(choice.Name, tx, ct))
        {
            tx.MarkSettled();
            var (st, sp) = EventSerializer.Serialize(new PaymentSettled(tx.Id, tx.CorrelationId));
            db.Outbox.Add(OutboxMessage.Create(st, sp));
        }
        else
        {
            account.Credit(tx.Amount);   // release the reservation
            tx.MarkFailed();
        }
        await db.SaveChangesAsync(ct);
        // If ChargeAsync THROWS (timeout, unknown outcome) the transaction stays Routed with funds reserved.
        // That needs gateway-side idempotency plus a reconciliation job: see docs/TEST_FINDINGS.md.
    }
}
