namespace CommonsInitializer.SeedWork;

//public abstract class Entity
//{

//    private readonly List<INotifications> _domainEventbus = new();

//    Guid _id;
//    int? _requestedHashCode;

//    public virtual Guid Id
//    {
//        get
//        {
//            return _id;
//        }
//        protected set
//        {
//            _id = value;
//        }
//    }

//    public IReadOnlyCollection<INotifications> DomainEventbus => 
//        _domainEventbus?.AsReadOnly() ?? new List<INotifications>().AsReadOnly();

//    public void AddDomainEvent(INotifications notification)
//    {
//        //_domainEventbus = _domainEventbus ?? new List<INotifications>();
//        _domainEventbus.Add(notification);
//    }

//    public void RemoveDomainEvent(INotifications notification)
//    {
//        if (_domainEventbus is null) return;
//        _domainEventbus.Remove(notification);
//    }

//    public void ClearDomainEvents()
//    {
//        _domainEventbus?.Clear();
//    }

//    public bool IsTransient()
//    {
//        return _id == default(Guid);
//    }


//    public override bool Equals(object? obj)
//    {
//        if (obj is null || !(obj is Entity))
//            return false;
//        if (ReferenceEquals(this, obj))
//            return true;
//        if (GetType() != obj.GetType())
//            return false;
//        Entity item = (Entity)obj;
//        if (item.IsTransient() || IsTransient())
//            return false;
//        else
//            return item.Id == Id;
//    }

//    public override int GetHashCode()
//    {
//        if (!IsTransient())
//        {
//            if (!_requestedHashCode.HasValue)
//                _requestedHashCode = Id.GetHashCode() ^ 31;
//            return _requestedHashCode.Value;
//        }
//        else
//            return base.GetHashCode();
//    }

//    public static bool operator ==(Entity left, Entity right)
//    {
//        if (Equals(left, null))
//            return (Equals(right, null)) ? true : false;
//        else
//            return left.Equals(right);
//    }

//    public static bool operator !=(Entity left, Entity right)
//    {
//        return !(left == right);
//    }
//}