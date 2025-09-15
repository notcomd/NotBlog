

using NotMediator;

namespace DomainCommonst;

public abstract class Entity
{

    Guid _id;
    int? _requestedHashCode;

    public virtual Guid Id
    {
        get
        {
            return _id;
        }
        protected set
        {
            _id = value;
        }
    }

    private readonly List<INotifications> _domainEvents = new();

    public IReadOnlyCollection<INotifications> DomainEvents => _domainEvents.AsReadOnly() ??
        new List<INotifications>().AsReadOnly();

    public void AddDomainEvent(INotifications notifications)
    {
        if (notifications is null) return;
        _domainEvents.Add(notifications);
    }


    public void RemoveDomainEvent(INotifications notifications)
    {
        if (notifications is null) return;
        _domainEvents.Remove(notifications);
    }


    public void ClearDomainEvents()
    {
        if (_domainEvents.Count > 0)
            _domainEvents.Clear();
    }

    protected bool Equals(Entity other)
    {
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Entity)obj);
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !Equals(left, right);
    }

    public bool IsTransient()
    {
        return Id == default;
    }



    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            _requestedHashCode ??= Id.GetHashCode() ^ 31; // XOR for random distribution
            return _requestedHashCode.Value;
        }
        else
        {
            return base.GetHashCode();
        }
    }
}