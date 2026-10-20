using FluentAssertions;
using SentinelPay.Infrastructure.Logging;
using Xunit;
namespace SentinelPay.Application.Tests;

public class LogQueryServiceTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    public LogQueryServiceTests() => File.WriteAllLines(Path.Combine(_dir, "sentinelpay-20261016.ndjson"),
    [
        """{"@t":"2026-10-16T05:00:00Z","@mt":"Payment {Id} accepted","Id":"p1","CorrelationId":"c-ok"}""",
        """{"@t":"2026-10-16T05:01:00Z","@mt":"Gateway timeout","@l":"Error","@x":"System.TimeoutException: gateway\n at X","CorrelationId":"c-bad"}""",
        """{"@t":"2026-10-16T05:02:00Z","@mt":"Gateway timeout","@l":"Error","@x":"System.TimeoutException: gateway\n at Y","CorrelationId":"c-bad2"}""",
        "not json at all"
    ]);
    public void Dispose() => Directory.Delete(_dir, true);

    [Fact] public void Filters_by_level_and_renders_template()
    {
        var sut = new LogQueryService(_dir);
        sut.Query(new("Error", null, null, null, null)).Should().HaveCount(2);
        sut.Query(new(null, "c-ok", null, null, null)).Single().Message.Should().Be("Payment p1 accepted");
    }
    [Fact] public void Summary_groups_repeated_errors()
    {
        var s = new LogQueryService(_dir).Summarize(TimeSpan.FromDays(3650));
        s.TopErrors.Single().Count.Should().Be(2); s.ByLevel["Error"].Should().Be(2);
    }
    [Fact] public void Corrupt_lines_are_skipped() => new LogQueryService(_dir).Query(new(null, null, null, null, null)).Should().HaveCount(3);
}
