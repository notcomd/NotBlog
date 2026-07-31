namespace Identity.Web.API.Application.Commands;

public class CreateRoleCommandHandler(
    IUserRoleRepository userRoleRepository,
    ILogger<CreateRoleCommandHandler> logger)
    : NotMediator.IRequestHandler<CreateRoleCommand, CreateRoleResult>
{
    public async Task<CreateRoleResult> Handler(
        CreateRoleCommand command, CancellationToken ct)
    {
        if (await userRoleRepository.IsUserRoleAsync(command.RoleName))
            throw new InvalidOperationException($"角色 '{command.RoleName}' 已存在");

        var role = new Roles(command.RoleName, command.RoleCode, command.RoleAuthority, RoleStatus.Normal, command.Attribute);

        await userRoleRepository.AddByUserRoleAsync(role);
        await userRoleRepository.UnitOfWork.SavaEntitiesAsync(ct);

        logger.LogInformation("[CreateRole] 创建成功: Name={Name}, Code={Code}",
            role.RoleName, role.RoleCode);

        return new CreateRoleResult(role.RoleGuid, role.RoleName, role.RoleCode);
    }
}

public class UpdateRoleCommandHandler(
    IUserRoleRepository userRoleRepository,
    ILogger<UpdateRoleCommandHandler> logger)
    : NotMediator.IRequestHandler<UpdateRoleCommand, bool>
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

        await userRoleRepository.UnitOfWork.SavaEntitiesAsync(ct);

        logger.LogInformation("[UpdateRole] 更新成功: Id={Id}", command.RoleGuid);
        return true;
    }
}

public class DeleteRoleCommandHandler(
    IUserRoleRepository userRoleRepository,
    ILogger<DeleteRoleCommandHandler> logger)
    : NotMediator.IRequestHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handler(
        DeleteRoleCommand command, CancellationToken ct)
    {
        var role = await userRoleRepository.FindByUserRoleAsync(command.RoleGuid)
            ?? throw new InvalidOperationException($"角色 '{command.RoleGuid}' 不存在");

        role.ResetByRoleStatus(RoleStatus.Deleted);
        role.SoftDelete(true);
        await userRoleRepository.UpByUserRoleAsync(role);
        await userRoleRepository.UnitOfWork.SavaEntitiesAsync(ct);

        logger.LogInformation("[DeleteRole] 软删除成功: Id={Id}", command.RoleGuid);
        return true;
    }
}

// Helper: SoftDelete extension for Roles
file static class RolesExtensions
{
    public static void SoftDelete(this Roles role, bool deleted)
    {
        typeof(Roles).GetProperty(nameof(Roles.IsDeleted))!.SetValue(role, deleted);
    }
}
