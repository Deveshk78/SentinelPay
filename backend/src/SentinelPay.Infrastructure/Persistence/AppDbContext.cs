using Microsoft.EntityFrameworkCore;
using SentinelPay.Application.Abstractions;
using SentinelPay.Domain.Entities;
namespace SentinelPay.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AgentAuditLog> AgentAuditLogs => Set<AgentAuditLog>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>(e => {
            e.ToTable("accounts"); e.Property(x => x.Balance).HasPrecision(18, 2); e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16); e.HasIndex(x => x.OwnerId);
            e.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();   // optimistic concurrency
        });
        b.Entity<Transaction>(e => {
            e.ToTable("transactions"); e.Property(x => x.Amount).HasPrecision(18, 2); e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.MerchantId).HasMaxLength(64); e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.CorrelationId).HasMaxLength(64); e.Property(x => x.Gateway).HasMaxLength(32);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.RiskDecision).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.RiskStage).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => new { x.AccountId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.AccountId, x.CreatedAt });
            e.HasIndex(x => x.CorrelationId);
            e.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        });
        b.Entity<AgentAuditLog>(e => {
            e.ToTable("agent_audit_logs"); e.Property(x => x.AgentName).HasMaxLength(64); e.Property(x => x.ModelId).HasMaxLength(64);
            e.Property(x => x.InputHash).HasMaxLength(64); e.Property(x => x.Rationale).HasMaxLength(2000);
            e.Property(x => x.Stage).HasConversion<string>().HasMaxLength(16); e.Property(x => x.Decision).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => x.TransactionId); e.HasIndex(x => x.CorrelationId);
        });
        b.Entity<OutboxMessage>(e => {
            e.ToTable("outbox_messages"); e.Property(x => x.Type).HasMaxLength(64); e.Property(x => x.Payload).HasColumnType("jsonb");
            e.HasIndex(x => new { x.ProcessedAt, x.CreatedAt });
        });
    }
}
