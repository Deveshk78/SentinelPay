using Microsoft.EntityFrameworkCore;
using SentinelPay.Application.Risk;
using SentinelPay.Domain.Entities;
using SentinelPay.Domain.Enums;
namespace SentinelPay.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<AgentAuditLog> AgentAuditLogs { get; }
    DbSet<OutboxMessage> Outbox { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
public interface IRiskScoringService { Task<RiskAssessment> ScoreAsync(RiskContext ctx, RiskStage stage, CancellationToken ct); }
public interface IIdempotencyStore { Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct); }
public interface IEventPublisher { Task PublishAsync(string type, string payload, string correlationId, CancellationToken ct); }
public sealed record GatewayChoice(string Name, string Reason);
public interface IPaymentGatewayRouter { Task<GatewayChoice> SelectAsync(Transaction tx, CancellationToken ct); }
public interface IGatewayClient { Task<bool> ChargeAsync(string gateway, Transaction tx, CancellationToken ct); }
