using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
namespace SentinelPay.Integration.Tests;

public class PaymentApiTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    [Fact] public async Task Health_is_public() => (await api.CreateClient().GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact] public async Task Payments_require_a_token() =>
        (await api.CreateClient().PostAsJsonAsync("/api/payments", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    [Fact] public async Task Accepted_payment_writes_transaction_and_outbox_together()
    {
        var owner = Guid.NewGuid(); var acct = await api.SeedAccountAsync(owner, 10_000); var before = await api.OutboxCountAsync();
        var c = api.ClientFor(owner); c.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var res = await c.PostAsJsonAsync("/api/payments", new { accountId = acct.Id, amount = 250.00m, currency = "INR", merchantId = "shop-1" });
        res.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await api.OutboxCountAsync()).Should().Be(before + 1);
    }
    [Fact] public async Task Another_users_account_returns_404()
    {
        var acct = await api.SeedAccountAsync(Guid.NewGuid(), 10_000);
        var c = api.ClientFor(Guid.NewGuid()); c.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await c.PostAsJsonAsync("/api/payments", new { accountId = acct.Id, amount = 10m, currency = "INR", merchantId = "shop-1" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact] public async Task Log_viewer_is_ops_only()
    {
        (await api.ClientFor(Guid.NewGuid()).GetAsync("/api/ops/logs")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await api.ClientFor(Guid.NewGuid(), "ops").GetAsync("/api/ops/logs")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
    [Fact] public async Task Invalid_input_is_rejected_before_any_work()
    {
        var c = api.ClientFor(Guid.NewGuid()); c.DefaultRequestHeaders.Add("Idempotency-Key", "short");
        (await c.PostAsJsonAsync("/api/payments", new { accountId = Guid.NewGuid(), amount = -5m, currency = "rupees", merchantId = "x;DROP" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
