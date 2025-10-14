namespace DomainCommon;

public abstract class ValueObject
{


    protected static bool EqualOperator(ValueObject left, ValueObject right)
    {
        return left?.Equals(right) ?? ReferenceEquals(right, null);
    }
    protected static bool NotEqualOperator(ValueObject left, ValueObject right)
    {
        return !EqualOperator(left, right);
    }


    protected abstract IEnumerable<object> GetAtomicValues();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }
        var other = obj as ValueObject;
        return GetAtomicValues().SequenceEqual(other!.GetAtomicValues());
    }

    public override int GetHashCode()
    {
        return GetAtomicValues()
            .Aggregate(1, (current, obj) => current * 23 + (obj?.GetHashCode() ?? 0));
    }

    public ValueObject? GetCopy()
    {
        return (ValueObject?)MemberwiseClone();
    }

}
