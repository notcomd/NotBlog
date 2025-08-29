namespace Identity.Web.API.Application.Models
{
    public class RequestChangeWithRoleModel
    {
        public RequestChangeWithRoleModel(string roleName, string? attribute, EnRoleAuthority roleAuthority, EnRoleStatus roleStatus, List<WithRoleClaimModel>? roleClaims)
        {
            RoleName = roleName;
            Attribute = attribute;
            RoleAuthority = roleAuthority;
            RoleStatus = roleStatus;
            RoleClaims = roleClaims;
        }

        public string RoleName { get; }

        public string? Attribute { get;}

        public EnRoleAuthority  RoleAuthority { get;}

        public EnRoleStatus RoleStatus { get;}

        public List<WithRoleClaimModel>? RoleClaims { get;  } = new();
    }
}
