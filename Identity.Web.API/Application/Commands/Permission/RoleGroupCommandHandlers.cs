namespace Identity.Web.API.Application.Commands;

public class CreateRoleGroupCommandHandler(
    IRoleGroupRepository roleGroupRepository,
    ILogger<CreateRoleGroupCommandHandler> logger)
    : NotMediator.IRequestHandler<CreateRoleGroupCommand, CreateRoleGroupResult>
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
    : NotMediator.IRequestHandler<UpdateRoleGroupCommand, bool>
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
    : NotMediator.IRequestHandler<DeleteRoleGroupCommand, bool>
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
