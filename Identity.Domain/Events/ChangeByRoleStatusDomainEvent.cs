namespace Identity.Domain.Events
{
    public record ChangeByRoleStatusDomainEvent : INotifications
    {
        public ChangeByRoleStatusDomainEvent(Guid roleGuid, string roleName, string attribute, string eventMessage, List<RoleClaim> roleClaim, DateTimeOffset changeTime)
        {
            RoleGuid = roleGuid;
            RoleName = roleName;
            Attribute = attribute;
            EventMessage = eventMessage;
            RoleClaim = roleClaim;
            ChangeTime = changeTime;
        }

        public Guid RoleGuid { get; set; }

        public string RoleName { get; set; }

        public string Attribute { get; set; }

        public string EventMessage { get; set; }

        public List<RoleClaim> RoleClaim { get; set; }

        public DateTimeOffset ChangeTime { get; set; }

    }
}