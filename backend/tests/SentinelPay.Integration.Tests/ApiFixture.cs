using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SentinelPay.Domain.Entities;
using SentinelPay.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;
namespace SentinelPay.Integration.Tests;

/// <summary>Real PostgreSQL + Redis in Docker. The broker is switched off here (outbox rows are asserted directly); add a RabbitMQ container for full-path tests.</summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Key = "integration-test-signing-key-32chars-min!";
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7-alpine").Build();

    public async Task InitializeAsync() { await Task.WhenAll(_pg.StartAsync(), _redis.StartAsync()); }
    public new async Task DisposeAsync() { await _pg.DisposeAsync(); await _redis.DisposeAsync(); await base.DisposeAsync(); }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder b)
    {
        // Environment variables are read at host build time, so they win over appsettings.json.
        void Set(string k, string v) => Environment.SetEnvironmentVariable(k, v);
        Set("ConnectionStrings__Postgres", _pg.GetConnectionString()); Set("ConnectionStrings__Redis", _redis.GetConnectionString());
        Set("Jwt__SigningKey", Key); Set("Messaging__RunConsumers", "false"); Set("Messaging__RunOutboxRelay", "false");
        Set("Risk__Mode", "Off"); Set("Logging__Directory", Path.Combine(Path.GetTempPath(), "sp-it-logs"));
    }

    public HttpClient ClientFor(Guid owner, params string[] roles)
    {
        var claims = new List<Claim> { new("sub", owner.ToString()) }; claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("sentinelpay", "sentinelpay-mobile", claims,
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256)));
        var c = CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); return c;
    }

    public async Task<Account> SeedAccountAsync(Guid owner, decimal balance)
    {
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var a = Account.Open(owner, "INR", balance); db.Accounts.Add(a); await db.SaveChangesAsync(); return a;
    }
    public async Task<int> OutboxCountAsync() { using var s = Services.CreateScope(); return await s.ServiceProvider.GetRequiredService<AppDbContext>().Outbox.CountAsync(); }
}
