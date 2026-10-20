using SentinelPay.Domain.Common;
using SentinelPay.Domain.Enums;
namespace SentinelPay.Domain.Entities;

public sealed class Account : Entity
{
    private Account() { }
    public Guid OwnerId { get; private set; }   // pseudonymous ID; names/emails never live in this service
    public string Currency { get; private set; } = "INR";
    public decimal Balance { get; private set; }
    public AccountStatus Status { get; private set; } = AccountStatus.Active;

    public static Account Open(Guid ownerId, string currency, decimal opening = 0m) =>
        new() { OwnerId = ownerId, Currency = currency.ToUpperInvariant(), Balance = opening };

    public bool CanAfford(decimal amount, string currency) =>
        Status == AccountStatus.Active && currency.Equals(Currency, StringComparison.OrdinalIgnoreCase) && Balance >= amount;

    public void Debit(decimal amount)
    {
        if (amount <= 0) throw new DomainException("Amount must be positive.");
        if (Status != AccountStatus.Active) throw new DomainException("Account is not active.");
        if (Balance < amount) throw new DomainException("Insufficient funds.");
        Balance -= amount; Touch();
    }
    public void Credit(decimal amount) { if (amount <= 0) throw new DomainException("Amount must be positive."); Balance += amount; Touch(); }
    public void Freeze() { Status = AccountStatus.Frozen; Touch(); }
}
