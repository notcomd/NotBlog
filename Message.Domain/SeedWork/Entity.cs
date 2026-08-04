using DomainCommons;
using NotMediator;

namespace Message.Domain.SeedWork;

public abstract class Entity : IHasDomainEvents
{
    private List<INotifications>? _domainEvents;
    private Guid _id;
    private int? _requestedHashCode;

    public virtual Guid Id
    {
        get => _id;
        protected set => _id = value;
    }

    public IReadOnlyCollection<INotifications> DomainEvents =>
        _domainEvents?.AsReadOnly() ?? new List<INotifications>().AsReadOnly();

    public void AddDomainEvent(INotifications notification)
    {
        _domainEvents ??= new List<INotifications>();
        _domainEvents.Add(notification);
    }

    public void RemoveDomainEvent(INotifications notification)
    {
        _domainEvents?.Remove(notification);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    public bool IsTransient()
    {
        return _id == Guid.Empty;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not Entity)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (GetType() != obj.GetType())
            return false;
        var item = (Entity)obj;
        if (item.IsTransient() || IsTransient())
            return false;
        return item.Id == Id;
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

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (Equals(left, null))
            return Equals(right, null);
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}
