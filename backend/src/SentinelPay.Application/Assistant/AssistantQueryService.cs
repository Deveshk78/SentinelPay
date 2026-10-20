using Microsoft.EntityFrameworkCore;
using SentinelPay.Application.Abstractions;
using SentinelPay.Domain.Enums;
namespace SentinelPay.Application.Assistant;

/// <summary>The model only ever produces this whitelisted filter. It never writes SQL and never sees other users' data.</summary>
public sealed record TransactionQuerySpec(TransactionStatus? Status, decimal? MinAmount, decimal? MaxAmount,
    DateTimeOffset? From, DateTimeOffset? To, string? MerchantId, int Limit = 20);
public sealed record TransactionView(Guid Id, decimal Amount, string Currency, string MerchantId, string Status, DateTimeOffset CreatedAt);
public sealed record AssistantAnswer(string Summary, TransactionQuerySpec Spec, IReadOnlyList<TransactionView> Items);

public interface IQuerySpecParser { Task<TransactionQuerySpec> ParseAsync(string question, DateTimeOffset now, CancellationToken ct); }

public sealed class AssistantQueryService(IQuerySpecParser parser, IAppDbContext db)
{
    public async Task<AssistantAnswer> AskAsync(Guid accountId, Guid callerOwnerId, string question, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > 500) throw new ArgumentException("Question must be 1-500 characters.");
        if (!await db.Accounts.AnyAsync(a => a.Id == accountId && a.OwnerId == callerOwnerId, ct)) throw new UnauthorizedAccessException();
        var s = await parser.ParseAsync(question, DateTimeOffset.UtcNow, ct);
        var q = db.Transactions.AsNoTracking().Where(t => t.AccountId == accountId);   // scope is forced here, not by the model
        if (s.Status is { } st) q = q.Where(t => t.Status == st);
        if (s.MinAmount is { } min) q = q.Where(t => t.Amount >= min);
        if (s.MaxAmount is { } max) q = q.Where(t => t.Amount <= max);
        if (s.From is { } f) q = q.Where(t => t.CreatedAt >= f);
        if (s.To is { } to) q = q.Where(t => t.CreatedAt <= to);
        if (!string.IsNullOrWhiteSpace(s.MerchantId)) q = q.Where(t => t.MerchantId == s.MerchantId);
        var items = await q.OrderByDescending(t => t.CreatedAt).Take(Math.Clamp(s.Limit, 1, 50))
            .Select(t => new TransactionView(t.Id, t.Amount, t.Currency, t.MerchantId, t.Status.ToString(), t.CreatedAt)).ToListAsync(ct);
        return new($"Found {items.Count} matching transaction(s).", s, items);
    }
}
