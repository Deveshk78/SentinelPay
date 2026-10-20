using System.Text;
using System.Threading.RateLimiting;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using SentinelPay.Agents;
using SentinelPay.Api.Endpoints;
using SentinelPay.Api.Security;
using SentinelPay.Application.Assistant;
using SentinelPay.Infrastructure;
using SentinelPay.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);
var logDir = Path.GetFullPath(builder.Configuration["Logging:Directory"] ?? "logs");

// Rotating, structured logs: daily files, 50 MB cap per file, 14 kept. The ops endpoints read these same files.
builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Information().MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext().Enrich.With<PiiMaskingEnricher>()
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), Path.Combine(logDir, "sentinelpay-.ndjson"), rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 50_000_000, rollOnFileSizeLimit: true, retainedFileCountLimit: 14, shared: true));

builder.Services.AddInfrastructure(builder.Configuration, logDir);

if (builder.Configuration.GetValue<bool>("Agents:Enabled"))
{
    var endpoint = new Uri(builder.Configuration["Agents:AzureOpenAIEndpoint"]!);
    builder.Services.AddSingleton<IChatClient>(_ => new AzureOpenAIClient(endpoint, new DefaultAzureCredential())   // managed identity: no API keys
        .GetChatClient(builder.Configuration["Agents:ModelId"]).AsIChatClient());
    builder.Services.AddSentinelAgents(builder.Configuration);
}
else builder.Services.AddScoped<IQuerySpecParser, DisabledParser>();

var jwt = builder.Configuration.GetSection("Jwt");
if ((jwt["SigningKey"]?.Length ?? 0) < 32) throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters (use Key Vault).");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new()
{
    ValidIssuer = jwt["Issuer"], ValidAudience = jwt["Audience"], ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
    ClockSkew = TimeSpan.FromSeconds(30), IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!))
});
builder.Services.AddAuthorizationBuilder().AddPolicy("Ops", p => p.RequireRole("ops"));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    RateLimitPartition<string> Per(HttpContext c, int permits) => RateLimitPartition.GetFixedWindowLimiter(
        c.User.FindFirst("sub")?.Value ?? c.Connection.RemoteIpAddress?.ToString() ?? "anon", _ => new() { PermitLimit = permits, Window = TimeSpan.FromMinutes(1) });
    o.AddPolicy("api", c => Per(c, 120));
    o.AddPolicy("assistant", c => Per(c, 20));    // model calls are costly: tighter limit
});
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseSentinelHeaders();
app.UseExceptionHandler(e => e.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    ctx.RequestServices.GetRequiredService<ILogger<Program>>().LogError(ex, "Unhandled exception");                       // details go to the log, never to the client (OWASP A05/A09)
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Something went wrong.", Status = 500, Extensions = { ["correlationId"] = ctx.Items["CorrelationId"] } });
}));
app.UseHsts();
app.UseSerilogRequestLogging();
app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapSentinelEndpoints();
if (app.Environment.IsDevelopment()) app.MapDevEndpoints();   // test-account and token helper, never in production

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();   // use migrations in non-dev
}
app.Run();

public partial class Program { }
internal sealed class DisabledParser : IQuerySpecParser
{
    public Task<TransactionQuerySpec> ParseAsync(string q, DateTimeOffset n, CancellationToken ct) => throw new ArgumentException("The assistant is not enabled.");
}
