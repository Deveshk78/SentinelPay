using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentinelPay.Application.Assistant;
using SentinelPay.Application.Risk;
namespace SentinelPay.Agents;

public static class AgentsServiceCollectionExtensions
{
    /// <summary>Requires an IChatClient registered by the host (Azure OpenAI in production).</summary>
    public static IServiceCollection AddSentinelAgents(this IServiceCollection s, IConfiguration cfg)
    {
        s.Configure<AgentOptions>(cfg.GetSection(AgentOptions.Section));
        s.AddScoped<IAgentRiskGraph, RiskGraph>();
        s.AddScoped<IQuerySpecParser, QuerySpecParser>();
        return s;
    }
}
