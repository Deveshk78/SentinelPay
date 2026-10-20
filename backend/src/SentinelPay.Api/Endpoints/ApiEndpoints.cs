using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Assistant;
using SentinelPay.Application.Payments;
using SentinelPay.Infrastructure.Logging;
using SentinelPay.Infrastructure.Persistence;
namespace SentinelPay.Api.Endpoints;

public sealed record CreatePaymentRequest(Guid AccountId, decimal Amount, string Currency, string MerchantId);
public sealed record AskRequest(Guid AccountId, string Question);

public static partial class ApiEndpoints
{
    private static Guid Caller(ClaimsPrincipal u) => Guid.TryParse((u.FindFirst("sub") ?? u.FindFirst(ClaimTypes.NameIdentifier))?.Value, out var g) ? g : throw new UnauthorizedAccessException();
    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")] private static partial Regex MerchantRx();
    [GeneratedRegex("^[A-Z]{3}$")] private static partial Regex CurrencyRx();

    public static void MapSentinelEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization().RequireRateLimiting("api");

        api.MapPost("/payments", async (CreatePaymentRequest r, HttpContext http, CreatePaymentHandler h, CancellationToken ct) =>
        {
            var key = http.Request.Headers["Idempotency-Key"].ToString();
            var errors = new Dictionary<string, string[]>();
            if (key.Length is < 8 or > 100) errors["Idempotency-Key"] = ["Required, 8-100 characters."];
            if (r.Amount is <= 0 or > 10_000_000) errors[nameof(r.Amount)] = ["Must be between 0.01 and 10,000,000."];
            if (!CurrencyRx().IsMatch(r.Currency ?? "")) errors[nameof(r.Currency)] = ["Use a 3-letter uppercase code."];
            if (!MerchantRx().IsMatch(r.MerchantId ?? "")) errors[nameof(r.MerchantId)] = ["Letters, digits, - and _ only."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var res = await h.HandleAsync(new(r.AccountId, Caller(http.User), decimal.Round(r.Amount, 2), r.Currency!, r.MerchantId!, key, (string)http.Items["CorrelationId"]!), ct);
            return res.Status switch
            {
                "NotFound" => Results.NotFound(res), "Conflict" => Results.Conflict(res),
                "Declined" or "Rejected" => Results.UnprocessableEntity(res),
                _ => Results.Accepted($"/api/payments/{res.TransactionId}", res)
            };
        });

        api.MapGet("/payments/{id:guid}", async (Guid id, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var owner = Caller(http.User);
            var t = await (from tx in db.Transactions.AsNoTracking() join a in db.Accounts.AsNoTracking() on tx.AccountId equals a.Id
                           where tx.Id == id && a.OwnerId == owner
                           select new TransactionView(tx.Id, tx.Amount, tx.Currency, tx.MerchantId, tx.Status.ToString(), tx.CreatedAt)).FirstOrDefaultAsync(ct);
            return t is null ? Results.NotFound() : Results.Ok(t);
        });

        api.MapPost("/assistant/query", async (AskRequest r, HttpContext http, AssistantQueryService svc, CancellationToken ct) =>
        {
            try { return Results.Ok(await svc.AskAsync(r.AccountId, Caller(http.User), r.Question, ct)); }
            catch (ArgumentException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.NotFound(); }
        }).RequireRateLimiting("assistant");

        // ---- Operations: rotating-log viewer + agent audit trail (role "ops") ----
        var ops = api.MapGroup("/ops").RequireAuthorization("Ops");
        ops.MapGet("/logs", (string? level, string? correlationId, string? q, DateTimeOffset? from, DateTimeOffset? to, int? take, LogQueryService logs) =>
            Results.Ok(logs.Query(new(level, correlationId, q, from, to, take ?? 100))));
        ops.MapGet("/logs/summary", (int? hours, LogQueryService logs) => Results.Ok(logs.Summarize(TimeSpan.FromHours(Math.Clamp(hours ?? 24, 1, 168)))));
        ops.MapGet("/audit/{transactionId:guid}", async (Guid transactionId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.AgentAuditLogs.AsNoTracking().Where(a => a.TransactionId == transactionId).OrderBy(a => a.CreatedAt).ToListAsync(ct)));
    }
}
