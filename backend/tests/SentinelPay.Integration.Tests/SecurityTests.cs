using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SentinelPay.Infrastructure.Persistence;
using Xunit;
namespace SentinelPay.Integration.Tests;

public class SecurityTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private HttpClient WithToken(string key, string audience, DateTime notBefore, DateTime expires)
    {
        var jwt = new JwtSecurityToken("sentinelpay", audience, [new Claim("sub", Guid.NewGuid().ToString())], notBefore, expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        var c = api.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt)); return c;
    }
    private static readonly string Other = "a-completely-different-signing-key-of-32+chars";

    [Fact] public async Task Expired_token_is_rejected()
        => (await WithToken(ApiFixture.Key, "sentinelpay-mobile", DateTime.UtcNow.AddMinutes(-20), DateTime.UtcNow.AddMinutes(-10)).GetAsync("/api/payments/" + Guid.NewGuid()))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    [Fact] public async Task Token_signed_with_another_key_is_rejected()
        => (await WithToken(Other, "sentinelpay-mobile", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5)).GetAsync("/api/payments/" + Guid.NewGuid()))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    [Fact] public async Task Token_for_another_audience_is_rejected()
        => (await WithToken(ApiFixture.Key, "someone-else", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5)).GetAsync("/api/payments/" + Guid.NewGuid()))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    [Fact] public async Task Security_headers_are_present_on_every_response()
    {
        var r = await api.CreateClient().GetAsync("/health");
        r.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        r.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
        r.Headers.GetValues("Referrer-Policy").Should().Contain("no-referrer");
        r.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'none'");
        r.Headers.CacheControl!.NoStore.Should().BeTrue();
    }
    [Fact] public async Task A_valid_correlation_id_is_echoed_and_a_hostile_one_is_replaced()
    {
        var c = api.CreateClient();
        var good = new HttpRequestMessage(HttpMethod.Get, "/health"); good.Headers.Add("X-Correlation-Id", "abcdef-123456");
        (await c.SendAsync(good)).Headers.GetValues("X-Correlation-Id").Single().Should().Be("abcdef-123456");
        var bad = new HttpRequestMessage(HttpMethod.Get, "/health"); bad.Headers.Add("X-Correlation-Id", "x\"; DROP TABLE --");
        var echoed = (await c.SendAsync(bad)).Headers.GetValues("X-Correlation-Id").Single();
        echoed.Should().MatchRegex("^[a-f0-9]{32}$");
    }
    [Fact] public async Task The_assistant_endpoint_is_rate_limited_to_20_per_minute_per_user()
    {
        var c = api.ClientFor(Guid.NewGuid());
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 25; i++) codes.Add((await c.PostAsJsonAsync("/api/assistant/query", new { accountId = Guid.NewGuid(), question = "hello" })).StatusCode);
        codes.Take(20).Should().NotContain(HttpStatusCode.TooManyRequests);
        codes.Skip(20).Should().Contain(HttpStatusCode.TooManyRequests);
    }
    [Fact] public async Task Many_parallel_requests_with_one_idempotency_key_create_exactly_one_transaction()
    {
        var owner = Guid.NewGuid(); var acct = await api.SeedAccountAsync(owner, 100_000); var key = "race-" + Guid.NewGuid();
        var tasks = Enumerable.Range(0, 8).Select(async _ =>
        {
            var c = api.ClientFor(owner); c.DefaultRequestHeaders.Add("Idempotency-Key", key);
            return (await c.PostAsJsonAsync("/api/payments", new { accountId = acct.Id, amount = 10m, currency = "INR", merchantId = "shop-1" })).StatusCode;
        });
        var results = await Task.WhenAll(tasks);
        results.Should().OnlyContain(s => s == HttpStatusCode.Accepted || s == HttpStatusCode.Conflict);
        using var scope = api.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Transactions.CountAsync(t => t.IdempotencyKey == key)).Should().Be(1);
    }
}
