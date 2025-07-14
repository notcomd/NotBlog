namespace Identity.Domain.Events
{
    public class UserStatusChangeDomainEvent : INotifications
    {

        public UserStatusChangeDomainEvent(Guid userGuid, UserStatus userStatus)
        {
            UserGuid = userGuid;
            UserStatus = userStatus;
        }
        public Guid UserGuid { get; }

        public UserStatus UserStatus { get; }
    }
}