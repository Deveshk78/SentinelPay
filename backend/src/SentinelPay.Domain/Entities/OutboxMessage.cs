using SentinelPay.Domain.Common;
namespace SentinelPay.Domain.Entities;

public sealed class OutboxMessage : Entity
{
    private OutboxMessage() { }
    public string Type { get; private set; } = "";
    public string Payload { get; private set; } = "";
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }
    public static OutboxMessage Create(string type, string payload) => new() { Type = type, Payload = payload };
    public void MarkProcessed() { ProcessedAt = DateTimeOffset.UtcNow; Error = null; }
    public void MarkFailed(string error) { Attempts++; Error = error.Length > 500 ? error[..500] : error; }
}
