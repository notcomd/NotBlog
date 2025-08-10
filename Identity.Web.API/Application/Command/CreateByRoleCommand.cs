namespace Identity.Web.API.Application.Command;

public sealed record CreateByRoleCommand : IRequest<bool>
{

    public string RoleName { get; set; }

    public string? Attribute { get; set; }

    public EnumRoleAuthority RoleAuthority { get; set; }

    public EnumRoleStatus RoleStatus { get; set; }

    public List<RoleClaim> RoleClaims { get; set; } = new List<RoleClaim>();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;


    public CreateByRoleCommand(string roleName, string? attribute, EnumRoleAuthority roleAuthority, EnumRoleStatus roleStatus, DateTimeOffset createdAt)
    {
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        Attribute = attribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;
        CreatedAt = createdAt == default ? DateTimeOffset.UtcNow : createdAt;
    }

    public void AddRoleClaim(RoleClaim roleClaim)
    {
        ArgumentNullException.ThrowIfNull(roleClaim);
        RoleClaims.Add(roleClaim);
    }


}

