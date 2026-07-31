namespace Identity.Web.API.Application.Commands;

// ─── Permission Commands ───

public record CreatePermissionCommand(
    string PermissionCode,
    string PermissionName,
    string PermissionType,
    string? MenuPath = null,
    string? ApiMethod = null,
    string? ApiUrl = null
) : IRequest<CreatePermissionResult>, ILoggableCommand
{
    public string IdProperty => nameof(PermissionCode);
    public string IdValue => PermissionCode;
}

public record CreatePermissionResult(Guid PermissionId, string PermissionCode);

public record UpdatePermissionCommand(
    Guid PermissionId,
    string PermissionName,
    string PermissionType,
    string? MenuPath = null,
    string? ApiMethod = null,
    string? ApiUrl = null
) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(PermissionId);
    public string IdValue => PermissionId.ToString();
}

public record DeletePermissionCommand(Guid PermissionId)
    : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(PermissionId);
    public string IdValue => PermissionId.ToString();
}

// ─── RoleGroup Commands ───

public record CreateRoleGroupCommand(
    string RoleGroupName,
    string RoleGroupCode
) : IRequest<CreateRoleGroupResult>, ILoggableCommand
{
    public string IdProperty => nameof(RoleGroupCode);
    public string IdValue => RoleGroupCode;
}

public record CreateRoleGroupResult(Guid RoleGroupGuid, string RoleGroupName, string RoleGroupCode);

public record UpdateRoleGroupCommand(
    Guid RoleGroupGuid,
    string? RoleGroupName,
    string? RoleGroupCode
) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(RoleGroupGuid);
    public string IdValue => RoleGroupGuid.ToString();
}

public record DeleteRoleGroupCommand(Guid RoleGroupGuid)
    : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(RoleGroupGuid);
    public string IdValue => RoleGroupGuid.ToString();
}

// ─── Role Commands ───

public record CreateRoleCommand(
    string RoleName,
    string RoleCode,
    RoleAuthority RoleAuthority = RoleAuthority.User,
    string? Attribute = null
) : IRequest<CreateRoleResult>, ILoggableCommand
{
    public string IdProperty => nameof(RoleCode);
    public string IdValue => RoleCode;
}

public record CreateRoleResult(Guid RoleGuid, string RoleName, string RoleCode);

public record UpdateRoleCommand(
    Guid RoleGuid,
    string? RoleName,
    string? Attribute,
    RoleStatus? RoleStatus = null
) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(RoleGuid);
    public string IdValue => RoleGuid.ToString();
}

public record DeleteRoleCommand(Guid RoleGuid)
    : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(RoleGuid);
    public string IdValue => RoleGuid.ToString();
}
