using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentinelPay.Domain.Entities;
using SentinelPay.Domain.Enums;
using SentinelPay.Infrastructure.Persistence;
using Xunit;
namespace SentinelPay.Integration.Tests;

/// <summary>Applies scripts/audit_immutable.sql to a real database and proves the audit table is append-only.</summary>
public class AuditTriggerTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Script = """
        CREATE OR REPLACE FUNCTION forbid_audit_change() RETURNS trigger AS $$
        BEGIN RAISE EXCEPTION 'agent_audit_logs is append-only'; END; $$ LANGUAGE plpgsql;
        DROP TRIGGER IF EXISTS agent_audit_logs_immutable ON agent_audit_logs;
        CREATE TRIGGER agent_audit_logs_immutable BEFORE UPDATE OR DELETE ON agent_audit_logs
        FOR EACH ROW EXECUTE FUNCTION forbid_audit_change();
        """;

    [Fact] public async Task Audit_rows_can_be_added_but_never_changed_or_deleted()
    {
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(Script);
        db.AgentAuditLogs.Add(AgentAuditLog.Record("corr-12345678", Guid.NewGuid(), "RiskGraph", RiskStage.Deep, "hash", RiskDecision.Approve, 10, "ok", "model-x", 5));
        await db.SaveChangesAsync();   // inserts still work

        var update = () => db.Database.ExecuteSqlRawAsync("UPDATE agent_audit_logs SET \"Score\" = 0");
        var delete = () => db.Database.ExecuteSqlRawAsync("DELETE FROM agent_audit_logs");
        (await update.Should().ThrowAsync<Exception>()).Which.ToString().Should().Contain("append-only");
        (await delete.Should().ThrowAsync<Exception>()).Which.ToString().Should().Contain("append-only");
    }
}
