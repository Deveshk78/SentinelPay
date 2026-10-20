using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Risk;
using SentinelPay.Domain.Enums;
using SentinelPay.Infrastructure.Risk;
using StackExchange.Redis;
using Xunit;
namespace SentinelPay.Application.Tests;

/// <summary>Fault injection at the model boundary: slow, broken or absent agents must never break a payment.</summary>
public class RiskScoringRouterTests
{
    private readonly IAgentRiskGraph _graph = Substitute.For<IAgentRiskGraph>();
    private static RiskContext Ctx(decimal amount = 500) => new(Guid.NewGuid(), Guid.NewGuid(), amount, "INR", "shop-1", "corr-12345678");
    private static RiskAssessment AgentResult() => new(42, "RiskGraph", "model-x", "agent said so", "h", 9);

    private static RuleBasedRiskScorer Rules(long velocity = 1)
    {
        var db = Substitute.For<IDatabase>();
        db.StringIncrementAsync(Arg.Any<RedisKey>(), Arg.Any<long>(), Arg.Any<CommandFlags>()).Returns(Task.FromResult(velocity));
        var redis = Substitute.For<IConnectionMultiplexer>(); redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return new(redis);
    }
    private RiskScoringRouter Router(bool withGraph = true, bool syncUsesAgent = false, long velocity = 1)
    {
        var o = Substitute.For<IOptionsMonitor<RiskOptions>>(); o.CurrentValue.Returns(new RiskOptions { SyncUsesAgent = syncUsesAgent });
        return new(Rules(velocity), withGraph ? [_graph] : [], o, NullLogger<RiskScoringRouter>.Instance);
    }

    [Fact] public async Task Deep_stage_uses_the_agent_graph()
    {
        _graph.RunAsync(default!, default).ReturnsForAnyArgs(AgentResult());
        (await Router().ScoreAsync(Ctx(), RiskStage.Deep, default)).Agent.Should().Be("RiskGraph");
    }
    [Fact] public async Task Broken_agent_falls_back_to_rules_and_says_so()
    {
        _graph.RunAsync(default!, default).ReturnsForAnyArgs(Task.FromException<RiskAssessment>(new InvalidOperationException("model down")));
        var r = await Router().ScoreAsync(Ctx(), RiskStage.Deep, default);
        r.Agent.Should().Be("RuleScorer"); r.Rationale.Should().StartWith("Agent fallback: ");
    }
    [Fact] public async Task Cancellation_is_not_swallowed_by_the_fallback()
    {
        _graph.RunAsync(default!, default).ReturnsForAnyArgs(Task.FromException<RiskAssessment>(new OperationCanceledException()));
        await Router().Invoking(r => r.ScoreAsync(Ctx(), RiskStage.Deep, default)).Should().ThrowAsync<OperationCanceledException>();
    }
    [Fact] public async Task Missing_agent_graph_still_scores_with_rules()
    {
        (await Router(withGraph: false).ScoreAsync(Ctx(), RiskStage.Deep, default)).Agent.Should().Be("RuleScorer");
    }
    [Fact] public async Task Fast_path_uses_rules_by_default_and_never_calls_the_model()
    {
        (await Router().ScoreAsync(Ctx(), RiskStage.SyncFast, default)).Agent.Should().Be("RuleScorer");
        await _graph.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }
    [Fact] public async Task Fast_path_uses_the_agent_only_when_configured()
    {
        _graph.RunAsync(default!, default).ReturnsForAnyArgs(AgentResult());
        (await Router(syncUsesAgent: true).ScoreAsync(Ctx(), RiskStage.SyncFast, default)).Agent.Should().Be("RiskGraph");
    }
    [Fact] public async Task A_burst_of_payments_raises_the_score()
    {
        var calm = await Router(velocity: 1).ScoreAsync(Ctx(), RiskStage.SyncFast, default);
        var burst = await Router(velocity: 6).ScoreAsync(Ctx(), RiskStage.SyncFast, default);
        burst.Score.Should().BeGreaterThan(calm.Score); burst.Rationale.Should().Contain("payments in 60s");
    }
    [Fact] public async Task A_very_large_amount_raises_the_score()
    {
        (await Router().ScoreAsync(Ctx(60_000), RiskStage.SyncFast, default)).Score.Should().BeGreaterThanOrEqualTo(40);
    }
    [Fact] public async Task Score_never_exceeds_100()
    {
        (await Router(velocity: 50).ScoreAsync(Ctx(1_000_000), RiskStage.SyncFast, default)).Score.Should().BeLessThanOrEqualTo(100);
    }
}
