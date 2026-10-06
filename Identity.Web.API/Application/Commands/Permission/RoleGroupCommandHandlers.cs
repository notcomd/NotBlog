namespace Identity.Web.API.Application.Commands;

public class CreateRoleGroupCommandHandler(
    IRoleGroupRepository roleGroupRepository,
    ILogger<CreateRoleGroupCommandHandler> logger)
    : IRequestHandler<CreateRoleGroupCommand, CreateRoleGroupResult>
{
    public async Task<CreateRoleGroupResult> Handler(
        CreateRoleGroupCommand command, CancellationToken ct)
    {
        var roleGroup = new RoleGroup(command.RoleGroupName, command.RoleGroupCode);

        await roleGroupRepository.AddOneByRoleGroupAsync(roleGroup);
        await roleGroupRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[CreateRoleGroup] 创建成功: Name={Name}, Code={Code}",
            roleGroup.RoleGroupName, roleGroup.RoleGroupCode);

        return new CreateRoleGroupResult(
            roleGroup.RoleGroupGuid, roleGroup.RoleGroupName, roleGroup.RoleGroupCode);
    }
}

public class UpdateRoleGroupCommandHandler(
    IRoleGroupRepository roleGroupRepository,
    ILogger<UpdateRoleGroupCommandHandler> logger)
    : IRequestHandler<UpdateRoleGroupCommand, bool>
{
    public async Task<bool> Handler(
        UpdateRoleGroupCommand command, CancellationToken ct)
    {
        var all = await roleGroupRepository.GetAllAsync();
        var roleGroup = all.FirstOrDefault(r => r.RoleGroupGuid == command.RoleGroupGuid)
            ?? throw new InvalidOperationException($"角色组 '{command.RoleGroupGuid}' 不存在");

        if (roleGroup.IsDeleted)
            throw new InvalidOperationException("无法更新已删除的角色组");

        if (command.RoleGroupName is not null)
            roleGroup.UpdateRoleGroupInfo(command.RoleGroupName, command.RoleGroupCode ?? roleGroup.RoleGroupCode);

        await roleGroupRepository.UpdateOneByRoleGroupAsync(roleGroup);
        await roleGroupRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[UpdateRoleGroup] 更新成功: Id={Id}", command.RoleGroupGuid);
        return true;
    }
}

public class DeleteRoleGroupCommandHandler(
    IRoleGroupRepository roleGroupRepository,
    ILogger<DeleteRoleGroupCommandHandler> logger)
    : IRequestHandler<DeleteRoleGroupCommand, bool>
{
    public async Task<bool> Handler(
        DeleteRoleGroupCommand command, CancellationToken ct)
    {
        var all = await roleGroupRepository.GetAllAsync();
        var roleGroup = all.FirstOrDefault(r => r.RoleGroupGuid == command.RoleGroupGuid)
            ?? throw new InvalidOperationException($"角色组 '{command.RoleGroupGuid}' 不存在");

        roleGroup.SoftDelete(true);
        await roleGroupRepository.UpdateOneByRoleGroupAsync(roleGroup);
        await roleGroupRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[DeleteRoleGroup] 软删除成功: Id={Id}", command.RoleGroupGuid);
        return true;
    }
}

/// <summary>
/// 全量替换角色组直连权限（树形授权入口）。语义对齐 SetRolePermissionsCommandHandler：
/// 校验角色组存在、按 PermissionId 集合重建关联、保存。
/// 组权限判定同样经 PermissionChecker 1 秒进程内缓存生效，故不额外主动失效（与角色侧保持一致）。
/// </summary>
public class SetRoleGroupPermissionsCommandHandler(
    IRoleGroupRepository roleGroupRepository,
    IPermissionRepository permissionRepository,
    ILogger<SetRoleGroupPermissionsCommandHandler> logger)
    : IRequestHandler<SetRoleGroupPermissionsCommand, bool>
{
    public async Task<bool> Handler(SetRoleGroupPermissionsCommand command, CancellationToken ct)
    {
        var roleGroup = await roleGroupRepository.FindByRoleGroupWithPermissionsAsync(command.RoleGroupGuid)
            ?? throw new InvalidOperationException($"角色组 '{command.RoleGroupGuid}' 不存在");
        if (roleGroup.IsDeleted)
            throw new InvalidOperationException("无法为已删除的角色组分配权限");

        // 权限码集合按 id 校验存在性（已删除/不存在 → 明确报错，防静默丢授权）
        var ids = command.PermissionIds?.Distinct().ToList() ?? [];
        var allPermissions = await permissionRepository.GetAllAsync(ct);
        var byId = allPermissions.ToDictionary(p => p.PermissionId);
        var invalid = ids.Where(id => !byId.ContainsKey(id)).ToList();
        if (invalid.Count > 0)
            throw new InvalidOperationException($"权限不存在或已删除: {string.Join(", ", invalid.Take(5))}");

        var granted = ids.Select(id => byId[id]).ToList();
        roleGroup.ReplacePermissions(granted);

        await roleGroupRepository.UpdateOneByRoleGroupAsync(roleGroup);
        await roleGroupRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation(
            "[SetRoleGroupPermissions] 角色组 {GroupId} 权限已更新：{Count} 个权限节点（含目录授权）",
            command.RoleGroupGuid, granted.Count);
        return true;
    }
}
