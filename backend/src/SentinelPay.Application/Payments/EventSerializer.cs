using System.Text.Json;
using SentinelPay.Domain.Events;
namespace SentinelPay.Application.Payments;

public static class EventSerializer
{
    private static readonly Dictionary<string, Type> Map = new[] { typeof(PaymentRequested), typeof(PaymentDeclined), typeof(PaymentRouted), typeof(PaymentSettled) }
        .ToDictionary(t => t.Name);
    public static (string Type, string Payload) Serialize(IIntegrationEvent e) => (e.GetType().Name, JsonSerializer.Serialize(e, e.GetType()));
    public static IIntegrationEvent? Deserialize(string type, string payload) =>
        Map.TryGetValue(type, out var t) ? (IIntegrationEvent?)JsonSerializer.Deserialize(payload, t) : null;
}
