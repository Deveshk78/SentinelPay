using Microsoft.Extensions.Options;
using SentinelPay.Application.Abstractions;
using SentinelPay.Domain.Entities;
namespace SentinelPay.Infrastructure.Gateways;

public sealed class GatewayOptions
{
    public const string Section = "Gateways";
    public List<GatewayConfig> Providers { get; set; } = [new() { Name = "Sandbox", FeeBps = 150 }];
    public sealed class GatewayConfig { public string Name { get; set; } = ""; public int FeeBps { get; set; } public bool Enabled { get; set; } = true; }
}

/// <summary>Cheapest enabled gateway. The Agents project can replace this port with a model-assisted router (validated against this same whitelist).</summary>
public sealed class RuleBasedGatewayRouter(IOptionsMonitor<GatewayOptions> o) : IPaymentGatewayRouter
{
    public Task<GatewayChoice> SelectAsync(Transaction tx, CancellationToken ct)
    {
        var best = o.CurrentValue.Providers.Where(p => p.Enabled).OrderBy(p => p.FeeBps).FirstOrDefault()
                   ?? throw new InvalidOperationException("No gateway enabled.");
        return Task.FromResult(new GatewayChoice(best.Name, $"lowest fee ({best.FeeBps} bps)"));
    }
}

/// <summary>Replace with real PSP adapters (one class per provider, behind this port).</summary>
public sealed class SandboxGatewayClient : IGatewayClient
{
    public async Task<bool> ChargeAsync(string gateway, Transaction tx, CancellationToken ct) { await Task.Delay(50, ct); return true; }
}
