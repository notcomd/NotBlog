namespace Commons.SeedWork;

public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetAtomicValues();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = obj as ValueObject;
        return GetAtomicValues().SequenceEqual(other!.GetAtomicValues());
    }

    public override int GetHashCode()
    {
        return GetAtomicValues()
            .Select(x => x is not null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }
}
