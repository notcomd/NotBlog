namespace Identity.Web.API.Application.Commands;

public class CreateRoleCommandHandler(
    IUserRoleRepository userRoleRepository,
    ILogger<CreateRoleCommandHandler> logger)
    : IRequestHandler<CreateRoleCommand, CreateRoleResult>
{
    public async Task<CreateRoleResult> Handler(
        CreateRoleCommand command, CancellationToken ct)
    {
        if (await userRoleRepository.IsUserRoleAsync(command.RoleName))
            throw new InvalidOperationException($"角色 '{command.RoleName}' 已存在");

        var role = new Roles(command.RoleName, command.RoleCode, command.RoleAuthority, RoleStatus.Normal, command.Attribute);

        await userRoleRepository.AddByUserRoleAsync(role);
        await userRoleRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[CreateRole] 创建成功: Name={Name}, Code={Code}",
            role.RoleName, role.RoleCode);

        return new CreateRoleResult(role.RoleGuid, role.RoleName, role.RoleCode);
    }
}

public class UpdateRoleCommandHandler(
    IUserRoleRepository userRoleRepository,
    ILogger<UpdateRoleCommandHandler> logger)
    : IRequestHandler<UpdateRoleCommand, bool>
{
    public async Task<bool> Handler(
        UpdateRoleCommand command, CancellationToken ct)
    {
        var role = await userRoleRepository.FindByUserRoleAsync(command.RoleGuid)
            ?? throw new InvalidOperationException($"角色 '{command.RoleGuid}' 不存在");

        if (role.IsDeleted)
            throw new InvalidOperationException("无法更新已删除的角色");

        if (command.RoleName is not null)
            role.UpdateRoleInfo(command.RoleName, command.Attribute);

        if (command.RoleStatus.HasValue)
            role.ResetByRoleStatus(command.RoleStatus.Value);

        var updated = await userRoleRepository.UpByUserRoleAsync(role);
        if (!updated) throw new InvalidOperationException("角色更新失败");

        await userRoleRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[UpdateRole] 更新成功: Id={Id}", command.RoleGuid);
        return true;
    }
}

public class DeleteRoleCommandHandler(
    IUserRoleRepository userRoleRepository,
    IUserRepository userRepository,
    ITokenSessionService tokenSessionService,
    ILogger<DeleteRoleCommandHandler> logger)
    : IRequestHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handler(
        DeleteRoleCommand command, CancellationToken ct)
    {
        var role = await userRoleRepository.FindByUserRoleAsync(command.RoleGuid)
            ?? throw new InvalidOperationException($"角色 '{command.RoleGuid}' 不存在");

        role.ResetByRoleStatus(RoleStatus.Deleted);
        role.SoftDelete(true);
        await userRoleRepository.UpByUserRoleAsync(role);
        await userRoleRepository.UnitOfWork.SaveEntitiesAsync(ct);

        // 吊销持有该角色的用户会话（与 SetRolePermissions 同语义：删除角色后
        // 用户 token 内权限 claim 仍含该角色权限，必须强制重登才能撤权）
        try
        {
            var userGuids = await userRepository.FindUserGuidsByRoleAsync(command.RoleGuid, ct);
            foreach (var userGuid in userGuids)
            {
                try
                {
                    await tokenSessionService.RevokeAllSessionsAsync(userGuid, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "[DeleteRole] 吊销用户 {UserId} 会话失败（其 token 将依赖自然过期）", userGuid);
                }
            }

            if (userGuids.Count > 0)
                logger.LogInformation("[DeleteRole] 已吊销 {Count} 个持该角色用户的会话", userGuids.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DeleteRole] 反查持角色用户失败（其 token 将依赖自然过期）");
        }

        logger.LogInformation("[DeleteRole] 软删除成功: Id={Id}", command.RoleGuid);
        return true;
    }
}

/// <summary>
/// 全量替换角色直连权限（树形授权入口）。
/// 保存后吊销持有该角色的全部用户会话——JWT 权限已入 claim，撤权必须强制用户重登才能生效。
/// </summary>
public class SetRolePermissionsCommandHandler(
    IUserRoleRepository userRoleRepository,
    IPermissionRepository permissionRepository,
    IUserRepository userRepository,
    ITokenSessionService tokenSessionService,
    ILogger<SetRolePermissionsCommandHandler> logger)
    : IRequestHandler<SetRolePermissionsCommand, bool>
{
    public async Task<bool> Handler(SetRolePermissionsCommand command, CancellationToken ct)
    {
        var role = await userRoleRepository.FindByUserRoleWithPermissionsAsync(command.RoleGuid)
            ?? throw new InvalidOperationException($"角色 '{command.RoleGuid}' 不存在");
        if (role.IsDeleted || role.RoleStatus == RoleStatus.Deleted)
            throw new InvalidOperationException("无法为已删除的角色分配权限");

        // 权限码集合按 id 校验存在性（已删除/不存在 → 明确报错，防静默丢授权）
        var ids = command.PermissionIds?.Distinct().ToList() ?? [];
        var allPermissions = await permissionRepository.GetAllAsync(ct);
        var byId = allPermissions.ToDictionary(p => p.PermissionId);
        var invalid = ids.Where(id => !byId.ContainsKey(id)).ToList();
        if (invalid.Count > 0)
            throw new InvalidOperationException($"权限不存在或已删除: {string.Join(", ", invalid.Take(5))}");

        var granted = ids.Select(id => byId[id]).ToList();
        role.ReplacePermissions(granted);

        await userRoleRepository.UpByUserRoleAsync(role);
        await userRoleRepository.UnitOfWork.SaveEntitiesAsync(ct);

        // 方案 A：吊销持此角色用户全部会话（Redis 会话 + token 黑名单），强制重登刷新权限 claim。
        // ⚠️ 吊销失败只记 Error 不抛出：权限变更已提交，抛错会令命令 500 且幂等重试返回
        // "首次结果"导致吊销永不执行；剩余未吊销 token 依赖自然过期兜底。
        var userGuids = await userRepository.FindUserGuidsByRoleAsync(command.RoleGuid, ct);
        foreach (var userGuid in userGuids)
        {
            try
            {
                await tokenSessionService.RevokeAllSessionsAsync(userGuid, ct);
                logger.LogInformation(
                    "[SetRolePermissions] 角色 {RoleId} 权限已变更，吊销用户 {UserId} 全部会话",
                    command.RoleGuid, userGuid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[SetRolePermissions] 吊销用户 {UserId} 会话失败（其 token 将依赖自然过期）",
                    userGuid);
            }
        }

        logger.LogInformation(
            "[SetRolePermissions] 角色 {RoleId} 权限已更新：{Count} 个权限节点（含目录授权），吊销 {Users} 个用户会话",
            command.RoleGuid, granted.Count, userGuids.Count);
        return true;
    }
}

