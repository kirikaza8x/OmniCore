using OmniCore.Shared.Domain.Abstractions;
namespace OmniCore.Shared.Domain.ValueObjects;

public enum Currency
{
    USD,
    EUR,
    GBP,
    JPY,
    AUD,
    CAD
}

public sealed record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            return Error.Validation("Money.Negative", "Monetary amount cannot be negative.");
        }

        return new Money(amount, currency);
    }

    public static Money Zero(Currency currency = Currency.USD) => new(0, currency);

    public static Result<Money> Add(Money first, Money second)
    {
        if (first.Currency != second.Currency)
        {
            return Error.Validation("Money.CurrencyMismatch", "Cannot operate on different currencies.");
        }

        return new Money(first.Amount + second.Amount, first.Currency);
    }

    public static Result<Money> Subtract(Money first, Money second)
    {
        if (first.Currency != second.Currency)
        {
            return Error.Validation("Money.CurrencyMismatch", "Cannot operate on different currencies.");
        }

        if (first.Amount < second.Amount)
        {
            return Error.Validation("Money.InsufficientFunds", "Subtraction result cannot be negative.");
        }

        return new Money(first.Amount - second.Amount, first.Currency);
    }
}