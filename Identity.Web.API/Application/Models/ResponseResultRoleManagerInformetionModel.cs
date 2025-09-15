namespace Identity.Web.API.Application.Models;

public class ResponseResultRoleManagerInformetionModel{
    public ResponseResultRoleManagerInformetionModel(string roleName,string roleAttribute,EnRoleAuthority roleAuthority,EnRoleStatus roleStatus
    ,IEnumerable<WithResultRoleClaimDto> roleManagerInformetions){
        RoleName = roleName;
        RoleAttribute = roleAttribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;
        RoleManagerInformetions = roleManagerInformetions;
    }
    public string RoleName { get; }
    public string RoleAttribute { get; }
    public EnRoleAuthority RoleAuthority { get; }
    public EnRoleStatus RoleStatus { get; }
    public IEnumerable<WithResultRoleClaimDto> RoleManagerInformetions { get; }=Enumerable.Empty<WithResultRoleClaimDto>();
}