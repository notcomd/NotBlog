namespace Identity.Domain.Events
{
    public class UserStatusChangeDomainEvent : INotifications
    {

        public UserStatusChangeDomainEvent(Guid userGuid, EnumUserStatus userStatus)
        {
            UserGuid = userGuid;
            UserStatus = userStatus;
        }
        public Guid UserGuid { get; }

        public EnumUserStatus UserStatus { get; }
    }
}