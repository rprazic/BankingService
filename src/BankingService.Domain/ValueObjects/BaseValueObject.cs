namespace BankingService.Domain.ValueObjects;

public abstract class BaseValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return ((BaseValueObject)obj)
            .GetEqualityComponents()
            .SequenceEqual(GetEqualityComponents());
    }

    public override int GetHashCode()
        => GetEqualityComponents()
            .Aggregate(1, (current, obj) => current * 23 + (obj?.GetHashCode() ?? 0));

    public static bool operator ==(BaseValueObject? left, BaseValueObject? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(BaseValueObject? left, BaseValueObject? right)
        => !(left == right);
}