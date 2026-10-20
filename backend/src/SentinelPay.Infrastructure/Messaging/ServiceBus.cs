using System.Text;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Payments;
using SentinelPay.Domain.Events;
namespace SentinelPay.Infrastructure.Messaging;

internal static class ServiceBusFactory
{
    public static ServiceBusClient Create(MessagingOptions.ServiceBusOptions o) =>
        !string.IsNullOrEmpty(o.FullyQualifiedNamespace) ? new ServiceBusClient(o.FullyQualifiedNamespace, new DefaultAzureCredential())
                                                          : new ServiceBusClient(o.ConnectionString);
}

public sealed class ServiceBusEventPublisher(IOptions<MessagingOptions> options) : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client = ServiceBusFactory.Create(options.Value.ServiceBus);
    private ServiceBusSender? _sender;
    public async Task PublishAsync(string type, string payload, string correlationId, CancellationToken ct)
    {
        _sender ??= _client.CreateSender(options.Value.ServiceBus.Topic);
        await _sender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
            { Subject = type, CorrelationId = correlationId, ContentType = "application/json" }, ct);
    }
    public async ValueTask DisposeAsync() { if (_sender is not null) await _sender.DisposeAsync(); await _client.DisposeAsync(); }
}

/// <summary>Max delivery count and dead-lettering are configured on the subscription (see infra/azure).</summary>
public sealed class ServiceBusConsumer(IOptions<MessagingOptions> options, IServiceScopeFactory scopes, ILogger<ServiceBusConsumer> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var o = options.Value.ServiceBus;
        await using var client = ServiceBusFactory.Create(o);
        await using var processor = client.CreateProcessor(o.Topic, o.Subscription, new ServiceBusProcessorOptions { AutoCompleteMessages = false, MaxConcurrentCalls = 8 });
        processor.ProcessMessageAsync += async a =>
        {
            using var scope = scopes.CreateScope();
            var evt = EventSerializer.Deserialize(a.Message.Subject ?? "", a.Message.Body.ToString());
            if (evt is PaymentRequested pr) await scope.ServiceProvider.GetRequiredService<PaymentProcessingHandler>().HandleAsync(pr, a.CancellationToken);
            await a.CompleteMessageAsync(a.Message, a.CancellationToken);   // thrown exceptions abandon the message; Service Bus counts deliveries
        };
        processor.ProcessErrorAsync += a => { log.LogError(a.Exception, "Service Bus error from {Source}", a.ErrorSource); return Task.CompletedTask; };
        await processor.StartProcessingAsync(ct);
        await Task.Delay(Timeout.Infinite, ct);
    }
}
