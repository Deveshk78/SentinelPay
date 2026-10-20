using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Payments;
using SentinelPay.Domain.Events;
namespace SentinelPay.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(IOptions<MessagingOptions> options) : IEventPublisher, IAsyncDisposable
{
    internal const string Exchange = "sentinelpay.events";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _conn; private IChannel? _ch;

    private async Task<IChannel> ChannelAsync(CancellationToken ct)
    {
        if (_ch is { IsOpen: true }) return _ch;
        await _gate.WaitAsync(ct);
        try
        {
            if (_ch is { IsOpen: true }) return _ch;
            _conn = await RabbitMqConnection.CreateAsync(options.Value.RabbitMq, ct);
            _ch = await _conn.CreateChannelAsync(cancellationToken: ct);
            await _ch.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
            return _ch;
        }
        finally { _gate.Release(); }
    }

    public async Task PublishAsync(string type, string payload, string correlationId, CancellationToken ct)
    {
        var ch = await ChannelAsync(ct);
        var props = new BasicProperties { Persistent = true, ContentType = "application/json", Type = type, CorrelationId = correlationId };
        await ch.BasicPublishAsync(Exchange, type, mandatory: false, props, Encoding.UTF8.GetBytes(payload), ct);
    }
    public async ValueTask DisposeAsync() { if (_ch is not null) await _ch.CloseAsync(); if (_conn is not null) await _conn.CloseAsync(); }
}

internal static class RabbitMqConnection
{
    public static Task<IConnection> CreateAsync(MessagingOptions.RabbitOptions o, CancellationToken ct) =>
        new ConnectionFactory { HostName = o.Host, UserName = o.User, Password = o.Password }.CreateConnectionAsync(ct);
}

/// <summary>Consumes PaymentRequested. Failed messages are retried once, then dead-lettered.</summary>
public sealed class RabbitMqConsumer(IOptions<MessagingOptions> options, IServiceScopeFactory scopes, ILogger<RabbitMqConsumer> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var conn = await RabbitMqConnection.CreateAsync(options.Value.RabbitMq, ct);
        await using var ch = await conn.CreateChannelAsync(cancellationToken: ct);
        await ch.ExchangeDeclareAsync(RabbitMqEventPublisher.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await ch.ExchangeDeclareAsync("sentinelpay.dlx", ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await ch.QueueDeclareAsync("sentinelpay.dead", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await ch.QueueBindAsync("sentinelpay.dead", "sentinelpay.dlx", "", cancellationToken: ct);
        await ch.QueueDeclareAsync("sentinelpay.payments", durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "sentinelpay.dlx" }, cancellationToken: ct);
        await ch.QueueBindAsync("sentinelpay.payments", RabbitMqEventPublisher.Exchange, nameof(PaymentRequested), cancellationToken: ct);
        await ch.BasicQosAsync(0, 8, false, ct);

        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                using var scope = scopes.CreateScope();
                var evt = EventSerializer.Deserialize(ea.BasicProperties.Type ?? "", Encoding.UTF8.GetString(ea.Body.Span));
                if (evt is PaymentRequested pr)
                    await scope.ServiceProvider.GetRequiredService<PaymentProcessingHandler>().HandleAsync(pr, ct);
                await ch.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Processing failed (redelivered={R})", ea.Redelivered);
                await ch.BasicNackAsync(ea.DeliveryTag, false, requeue: !ea.Redelivered, ct);
            }
        };
        await ch.BasicConsumeAsync("sentinelpay.payments", autoAck: false, consumer, ct);
        await Task.Delay(Timeout.Infinite, ct);
    }
}
