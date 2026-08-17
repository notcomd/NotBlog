using Commons.Core;
using NotMediator.Abstractions;

namespace Commons.SeedWork;

public abstract class Entity<TId> : IHasDomainEvents where TId : struct, IEquatable<TId>
{
    private List<INotifications>? _domainEvents;
    private TId _id;
    private int? _requestedHashCode;

    public virtual TId Id
    {
        get => _id;
        protected set => _id = value;
    }

    public IReadOnlyCollection<INotifications> DomainEvents =>
        _domainEvents?.AsReadOnly() ?? new List<INotifications>().AsReadOnly();

    public void AddDomainEvent(INotifications notification)
    {
        _domainEvents = _domainEvents ?? [];
        _domainEvents.Add(notification);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    public bool IsTransient()
    {
        return EqualityComparer<TId>.Default.Equals(_id, default);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not Entity<TId>)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (GetType() != obj.GetType())
            return false;
        var item = (Entity<TId>)obj;
        if (item.IsTransient() || IsTransient())
            return false;
        return item.Id.Equals(Id);
    }

    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            if (!_requestedHashCode.HasValue)
                _requestedHashCode = Id.GetHashCode() ^ 31;
            return _requestedHashCode.Value;
        }
        return base.GetHashCode();
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}
