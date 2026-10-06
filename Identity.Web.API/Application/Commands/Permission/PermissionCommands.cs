namespace Identity.Web.API.Application.Commands;

// ─── Permission Commands ───

public record CreatePermissionCommand(
    string PermissionCode,
    string PermissionName,
    PermissionType PermissionType,
    Guid? ParentId = null,
    string? Url = null,
    string? Icon = null,
    int SortOrder = 0
) : IRequest<CreatePermissionResult>, ILoggableCommand
{
    public string IdProperty => nameof(PermissionCode);
    public string IdValue => PermissionCode;
}

public record CreatePermissionResult(Guid PermissionId, string PermissionCode);

/// <summary>
/// 更新权限。语义：PermissionName/PermissionType/Url/Icon/SortOrder 传 null 表示不修改
/// （Url/Icon 传空串表示清除）；ParentId 传 null 表示不修改父节点，Guid.Empty 表示移至根。
/// </summary>
public record UpdatePermissionCommand(
    Guid PermissionId,
    string? PermissionName = null,
    PermissionType? PermissionType = null,
    Guid? ParentId = null,
    string? Url = null,
    string? Icon = null,
    int? SortOrder = null
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

/// <summary>
/// 全量替换角色组的直连权限（树形授权：permissionIds 可含目录码与叶子码，
/// 授权目录 = 自动放行其全部子孙，判定由 PermissionChecker 前缀段匹配完成）。
/// </summary>
public record SetRoleGroupPermissionsCommand(Guid RoleGroupGuid, IReadOnlyList<Guid> PermissionIds)
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

/// <summary>
/// 全量替换角色的直连权限（树形授权：permissionIds 可含目录码与叶子码，
/// 授权目录 = 自动放行其全部子孙，判定由 PermissionChecker 前缀段匹配完成）。
/// 保存后自动吊销持有该角色的全部用户会话（强制重新登录以刷新 JWT 权限 claim）。
/// </summary>
public record SetRolePermissionsCommand(Guid RoleGuid, IReadOnlyList<Guid> PermissionIds)
    : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(RoleGuid);
    public string IdValue => RoleGuid.ToString();
}
