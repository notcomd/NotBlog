
namespace Identity.Web.API.Application.Commands;

public class CreatePermissionCommandHandler(
    IPermissionRepository permissionRepository,
    ILogger<CreatePermissionCommandHandler> logger)
    : IRequestHandler<CreatePermissionCommand, CreatePermissionResult>
{
    public async Task<CreatePermissionResult> Handler(
        CreatePermissionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.PermissionCode))
            throw new InvalidOperationException("权限码不能为空");

        if (await permissionRepository.CodeExistsAsync(command.PermissionCode, ct))
            throw new InvalidOperationException($"权限码 '{command.PermissionCode}' 已存在");

        // 挂载到指定父节点时，父节点必须存在且未删除
        if (command.ParentId is { } parentId)
        {
            var parent = await permissionRepository.FindByIdAsync(parentId, ct)
                ?? throw new InvalidOperationException($"父权限 '{parentId}' 不存在");
            if (parent.IsDeleted)
                throw new InvalidOperationException("父权限已删除，无法在其下创建");
        }

        var permission = new Permission(
            command.PermissionCode,
            command.PermissionName,
            command.PermissionType,
            command.ParentId,
            command.Url,
            command.Icon,
            command.SortOrder);

        await permissionRepository.AddAsync(permission, ct);
        await permissionRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[CreatePermission] 创建成功: Code={Code}, Id={Id}, Type={Type}, ParentId={ParentId}",
            permission.PermissionCode, permission.PermissionId, permission.PermissionType, permission.ParentId);

        return new CreatePermissionResult(permission.PermissionId, permission.PermissionCode);
    }
}

public class UpdatePermissionCommandHandler(
    IPermissionRepository permissionRepository,
    ILogger<UpdatePermissionCommandHandler> logger)
    : IRequestHandler<UpdatePermissionCommand, bool>
{
    public async Task<bool> Handler(
        UpdatePermissionCommand command, CancellationToken ct)
    {
        var permission = await permissionRepository.FindByIdAsync(command.PermissionId, ct)
            ?? throw new InvalidOperationException($"权限 '{command.PermissionId}' 不存在");

        if (permission.IsDeleted)
            throw new InvalidOperationException("无法更新已删除的权限");

        var name = command.PermissionName ?? permission.PermissionName;
        var type = command.PermissionType ?? permission.PermissionType;
        permission.ChangePermission(name, type, command.Url, command.Icon, command.SortOrder);

        // 父节点变更：null=不改，Guid.Empty=移至根，其他=挂到新父下（防成环）
        if (command.ParentId is { } newParent)
        {
            if (newParent == Guid.Empty)
            {
                permission.ChangeParent(null);
            }
            else
            {
                await EnsureNoCycleAsync(permissionRepository, permission.PermissionId, newParent, ct);
                permission.ChangeParent(newParent);
            }
        }

        await permissionRepository.UpdateAsync(permission, ct);
        await permissionRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[UpdatePermission] 更新成功: Id={Id}", command.PermissionId);
        return true;
    }

    /// <summary>
    /// 防成环：沿新父节点向上追溯，若链上遇到目标节点自身则拒绝
    /// （把节点挂到自己的子孙节点下会形成循环树）。
    /// </summary>
    private static async Task EnsureNoCycleAsync(
        IPermissionRepository repo, Guid permissionId, Guid newParentId, CancellationToken ct)
    {
        var cursor = newParentId;
        var hops = 0;
        while (cursor != Guid.Empty && cursor != permissionId && hops++ < 200)
        {
            var parent = await repo.FindByIdAsync(cursor, ct);
            if (parent is null)
                throw new InvalidOperationException($"父权限 '{cursor}' 不存在");
            cursor = parent.ParentId ?? Guid.Empty;
        }

        if (cursor == permissionId)
            throw new InvalidOperationException("不能将权限移动到自己的子孙节点下（会形成循环）");
    }
}

public class DeletePermissionCommandHandler(
    IPermissionRepository permissionRepository,
    ILogger<DeletePermissionCommandHandler> logger)
    : IRequestHandler<DeletePermissionCommand, bool>
{
    public async Task<bool> Handler(
        DeletePermissionCommand command, CancellationToken ct)
    {
        var permission = await permissionRepository.FindByIdAsync(command.PermissionId, ct)
            ?? throw new InvalidOperationException($"权限 '{command.PermissionId}' 不存在");

        // 树语义：存在未删除子节点时禁止删除（先处理子节点，防止树悬挂）
        var children = await permissionRepository.GetChildrenAsync(command.PermissionId, ct);
        if (children.Any(c => !c.IsDeleted))
            throw new InvalidOperationException("该权限下存在子权限，请先删除或移动子权限");

        var deleted = await permissionRepository.DeleteAsync(command.PermissionId, ct);
        if (!deleted)
            throw new InvalidOperationException($"权限 '{command.PermissionId}' 不存在");

        await permissionRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[DeletePermission] 软删除成功: Id={Id}", command.PermissionId);
        return true;
    }
}
