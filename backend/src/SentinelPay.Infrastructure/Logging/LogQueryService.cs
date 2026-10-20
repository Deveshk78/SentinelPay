using System.Text.Json;
using System.Text.RegularExpressions;
namespace SentinelPay.Infrastructure.Logging;

public sealed record LogEntry(DateTimeOffset Time, string Level, string Message, string? CorrelationId, string? Exception);
public sealed record LogQuery(string? MinLevel, string? CorrelationId, string? Text, DateTimeOffset? From, DateTimeOffset? To, int Take = 100);
public sealed record LogSummary(int Total, IReadOnlyDictionary<string, int> ByLevel, IReadOnlyList<ErrorGroup> TopErrors, IReadOnlyList<string> Files);
public sealed record ErrorGroup(string Signature, int Count, DateTimeOffset LastSeen, string? SampleCorrelationId);

/// <summary>Reads the rolling NDJSON files written by Serilog (compact JSON). Read-only, share-safe, capped. Powers the in-app log viewer.</summary>
public sealed partial class LogQueryService(string directory)
{
    private static readonly string[] Levels = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];
    private const int MaxFiles = 7, MaxLinesPerFile = 200_000;

    public IReadOnlyList<LogEntry> Query(LogQuery q)
    {
        var min = Math.Max(0, Array.FindIndex(Levels, l => l.Equals(q.MinLevel, StringComparison.OrdinalIgnoreCase)));
        return ReadAll(q.From).Where(e => Array.IndexOf(Levels, e.Level) >= min)
            .Where(e => q.CorrelationId is null || e.CorrelationId == q.CorrelationId)
            .Where(e => q.To is null || e.Time <= q.To)
            .Where(e => string.IsNullOrWhiteSpace(q.Text) || e.Message.Contains(q.Text, StringComparison.OrdinalIgnoreCase) || (e.Exception?.Contains(q.Text, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(e => e.Time).Take(Math.Clamp(q.Take, 1, 500)).ToList();
    }

    public LogSummary Summarize(TimeSpan window)
    {
        var all = ReadAll(DateTimeOffset.UtcNow - window).ToList();
        var groups = all.Where(e => e.Level is "Error" or "Fatal")
            .GroupBy(e => Signature(e)).Select(g => new ErrorGroup(g.Key, g.Count(), g.Max(x => x.Time), g.OrderByDescending(x => x.Time).First().CorrelationId))
            .OrderByDescending(g => g.Count).Take(10).ToList();
        return new(all.Count, all.GroupBy(e => e.Level).ToDictionary(g => g.Key, g => g.Count()), groups, Files().Select(f => Path.GetFileName(f)).ToList()!);
    }

    private IEnumerable<string> Files() => Directory.Exists(directory)
        ? Directory.EnumerateFiles(directory, "sentinelpay-*.ndjson").OrderByDescending(f => f).Take(MaxFiles) : [];

    private IEnumerable<LogEntry> ReadAll(DateTimeOffset? from)
    {
        foreach (var file in Files())
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);   // the writer keeps the file open
            using var sr = new StreamReader(fs);
            var n = 0; string? line;
            while ((line = sr.ReadLine()) is not null && n++ < MaxLinesPerFile)
            {
                LogEntry? e = null;
                try { e = Parse(line); } catch (JsonException) { }
                if (e is not null && (from is null || e.Time >= from)) yield return e;
            }
        }
    }

    private static LogEntry? Parse(string line)
    {
        using var d = JsonDocument.Parse(line); var r = d.RootElement;
        if (!r.TryGetProperty("@t", out var t)) return null;
        var template = r.TryGetProperty("@mt", out var mt) ? mt.GetString() ?? "" : "";
        var msg = Token().Replace(template, m => r.TryGetProperty(m.Groups[1].Value, out var v) ? v.ToString() : m.Value);
        return new(t.GetDateTimeOffset(), r.TryGetProperty("@l", out var l) ? l.GetString() ?? "Information" : "Information", msg,
                   r.TryGetProperty("CorrelationId", out var c) ? c.GetString() : null, r.TryGetProperty("@x", out var x) ? x.GetString() : null);
    }
    private static string Signature(LogEntry e) => e.Exception?.Split('\n', 2)[0].Trim() ?? e.Message;
    [GeneratedRegex(@"\{(\w+)[^}]*\}")] private static partial Regex Token();
}
