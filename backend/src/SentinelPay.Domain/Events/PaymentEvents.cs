namespace SentinelPay.Domain.Events;

public interface IIntegrationEvent { Guid TransactionId { get; } string CorrelationId { get; } }
/// <summary>Written in the same DB commit as the transaction. DeepRiskRequired comes from the configured risk mode.</summary>
public sealed record PaymentRequested(Guid TransactionId, string CorrelationId, bool DeepRiskRequired) : IIntegrationEvent;
public sealed record PaymentDeclined(Guid TransactionId, string CorrelationId, string Reason) : IIntegrationEvent;
public sealed record PaymentRouted(Guid TransactionId, string CorrelationId, string Gateway) : IIntegrationEvent;
public sealed record PaymentSettled(Guid TransactionId, string CorrelationId) : IIntegrationEvent;
