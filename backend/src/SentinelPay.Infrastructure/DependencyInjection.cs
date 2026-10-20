using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentinelPay.Application.Abstractions;
using SentinelPay.Application.Assistant;
using SentinelPay.Application.Payments;
using SentinelPay.Application.Risk;
using SentinelPay.Infrastructure.Caching;
using SentinelPay.Infrastructure.Gateways;
using SentinelPay.Infrastructure.Logging;
using SentinelPay.Infrastructure.Messaging;
using SentinelPay.Infrastructure.Persistence;
using SentinelPay.Infrastructure.Risk;
using StackExchange.Redis;
namespace SentinelPay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration cfg, string logDirectory)
    {
        s.Configure<RiskOptions>(cfg.GetSection(RiskOptions.Section));
        s.Configure<MessagingOptions>(cfg.GetSection(MessagingOptions.Section));
        s.Configure<GatewayOptions>(cfg.GetSection(GatewayOptions.Section));
        var m = cfg.GetSection(MessagingOptions.Section).Get<MessagingOptions>() ?? new();

        s.AddDbContext<AppDbContext>(o => o.UseNpgsql(cfg.GetConnectionString("Postgres")));
        s.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        s.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(cfg.GetConnectionString("Redis")!));
        s.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

        s.AddSingleton<RuleBasedRiskScorer>();
        s.AddScoped<IRiskScoringService, RiskScoringRouter>();
        s.AddScoped<IPaymentGatewayRouter, RuleBasedGatewayRouter>();
        s.AddSingleton<IGatewayClient, SandboxGatewayClient>();

        s.AddScoped<CreatePaymentHandler>();
        s.AddScoped<PaymentProcessingHandler>();
        s.AddScoped<AssistantQueryService>();
        s.AddSingleton(new LogQueryService(logDirectory));

        if (m.Provider == BrokerProvider.ServiceBus) s.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();
        else s.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        // IEventPublisher is a singleton and OutboxRelay resolves it from a scope: fine.
        if (m.RunOutboxRelay) s.AddHostedService<OutboxRelay>();
        if (m.RunConsumers)
        {
            if (m.Provider == BrokerProvider.ServiceBus) s.AddHostedService<ServiceBusConsumer>();
            else s.AddHostedService<RabbitMqConsumer>();
        }
        return s;
    }
}
