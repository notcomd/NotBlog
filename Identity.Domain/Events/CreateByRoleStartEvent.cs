namespace Identity.Domain.Events
{
    public class CreateByRoleStartEvent : INotifications
    {
        public CreateByRoleStartEvent(Guid roleGuid, string roleName, string roleAttribute)
        {
            RoleGuid = roleGuid;
            RoleName = roleName;
            RoleAttribute = roleAttribute;
        }
        public Guid RoleGuid { get; }
        public string RoleName { get; }
        public string RoleAttribute { get; set; }


        public override string ToString()
        {
            return $"CreateByRoleStartEvent: {RoleGuid}, {RoleName},{RoleAttribute}";
        }
    }
}
