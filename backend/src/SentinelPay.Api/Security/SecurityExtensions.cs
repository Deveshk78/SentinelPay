using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;
namespace SentinelPay.Api.Security;

/// <summary>PII control for logs: sensitive property names are redacted and anything shaped like a card number is masked before any sink sees it.</summary>
public sealed partial class PiiMaskingEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> Sensitive = new(StringComparer.OrdinalIgnoreCase)
        { "email", "phone", "pan", "cardnumber", "cvv", "password", "token", "authorization", "name", "address", "dob" };
    public void Enrich(LogEvent e, ILogEventPropertyFactory f)
    {
        foreach (var p in e.Properties.ToList())
        {
            if (Sensitive.Contains(p.Key)) e.AddOrUpdateProperty(new LogEventProperty(p.Key, new ScalarValue("***")));
            else if (p.Value is ScalarValue { Value: string s })
            {
                var masked = Mail().Replace(Pan().Replace(s, "****"), "***@***");
                if (masked != s) e.AddOrUpdateProperty(new LogEventProperty(p.Key, new ScalarValue(masked)));
            }
        }
    }
    [GeneratedRegex(@"\b\d{13,19}\b")] private static partial Regex Pan();
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")] private static partial Regex Mail();
}

public static class SecurityMiddleware
{
    private static readonly Regex Corr = new("^[A-Za-z0-9-]{8,64}$", RegexOptions.Compiled);

    /// <summary>OWASP A05 headers + correlation id that ties request, outbox event, agent audit rows and log lines together.</summary>
    public static IApplicationBuilder UseSentinelHeaders(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        var incoming = ctx.Request.Headers["X-Correlation-Id"].ToString();
        var id = Corr.IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");
        ctx.Items["CorrelationId"] = id;
        ctx.Response.Headers["X-Correlation-Id"] = id;
        var h = ctx.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff"; h["X-Frame-Options"] = "DENY"; h["Referrer-Policy"] = "no-referrer";
        h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'"; h["Cache-Control"] = "no-store";
        h["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", id)) await next();
    });
}
