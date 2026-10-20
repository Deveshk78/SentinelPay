namespace SentinelPay.Infrastructure.Messaging;
public enum BrokerProvider { RabbitMq, ServiceBus }
public sealed class MessagingOptions
{
    public const string Section = "Messaging";
    public BrokerProvider Provider { get; set; } = BrokerProvider.RabbitMq;
    public bool RunConsumers { get; set; } = true;      // set false on an API-only host
    public bool RunOutboxRelay { get; set; } = true;
    public RabbitOptions RabbitMq { get; set; } = new();
    public ServiceBusOptions ServiceBus { get; set; } = new();
    public sealed class RabbitOptions { public string Host { get; set; } = "localhost"; public string User { get; set; } = "sentinel"; public string Password { get; set; } = ""; }
    public sealed class ServiceBusOptions
    {
        public string? FullyQualifiedNamespace { get; set; }   // preferred: managed identity, no secrets
        public string? ConnectionString { get; set; }          // dev only
        public string Topic { get; set; } = "sentinelpay-events";
        public string Subscription { get; set; } = "payments";
    }
}
