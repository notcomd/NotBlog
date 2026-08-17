namespace Identity.Web.API.Application.Commands;

public class CreatePermissionCommandHandler(
    IPermissionRepository permissionRepository,
    ILogger<CreatePermissionCommandHandler> logger)
    :  IRequestHandler<CreatePermissionCommand, CreatePermissionResult>
{
    public async Task<CreatePermissionResult> Handler(
        CreatePermissionCommand command, CancellationToken ct)
    {
        if (await permissionRepository.CodeExistsAsync(command.PermissionCode, ct))
            throw new InvalidOperationException($"权限码 '{command.PermissionCode}' 已存在");

        var permission = new Permission(
            command.PermissionCode,
            command.PermissionName,
            command.PermissionType,
            command.MenuPath,
            command.ApiMethod,
            command.ApiUrl);

        await permissionRepository.AddAsync(permission, ct);
        await permissionRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[CreatePermission] 创建成功: Code={Code}, Id={Id}",
            permission.PermissionCode, permission.PermissionId);

        return new CreatePermissionResult(permission.PermissionId, permission.PermissionCode);
    }
}

public class UpdatePermissionCommandHandler(
    IPermissionRepository permissionRepository,
    ILogger<UpdatePermissionCommandHandler> logger)
    :  IRequestHandler<UpdatePermissionCommand, bool>
{
    public async Task<bool> Handler(
        UpdatePermissionCommand command, CancellationToken ct)
    {
        var permission = await permissionRepository.FindByIdAsync(command.PermissionId, ct)
            ?? throw new InvalidOperationException($"权限 '{command.PermissionId}' 不存在");

        if (permission.IsDeleted)
            throw new InvalidOperationException("无法更新已删除的权限");

        permission.ChangePermission(
            string.Empty,
            command.PermissionName,
            command.PermissionType,
            command.MenuPath ?? string.Empty,
            command.ApiMethod ?? string.Empty,
            command.ApiUrl ?? string.Empty);

        await permissionRepository.UpdateAsync(permission, ct);
        await permissionRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[UpdatePermission] 更新成功: Id={Id}", command.PermissionId);
        return true;
    }
}

public class DeletePermissionCommandHandler(
    IPermissionRepository permissionRepository,
    ILogger<DeletePermissionCommandHandler> logger)
    :  IRequestHandler<DeletePermissionCommand, bool>
{
    public async Task<bool> Handler(
        DeletePermissionCommand command, CancellationToken ct)
    {
        var deleted = await permissionRepository.DeleteAsync(command.PermissionId, ct);
        if (!deleted)
            throw new InvalidOperationException($"权限 '{command.PermissionId}' 不存在");

        await permissionRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[DeletePermission] 软删除成功: Id={Id}", command.PermissionId);
        return true;
    }
}
