using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;



public class CreatePermissionIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<CreatePermissionCommand, CreatePermissionResult>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<CreatePermissionCommand, CreatePermissionResult>(logger, mediator, requestManagement)
{
    protected override CreatePermissionResult CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty);
}

public class UpdatePermissionIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<UpdatePermissionCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<UpdatePermissionCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest() => false;
}

public class DeletePermissionIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<DeletePermissionCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<DeletePermissionCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest() => false;
}

// ─── RoleGroup Idempotent Handlers ───

public class CreateRoleGroupIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<CreateRoleGroupCommand, CreateRoleGroupResult>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<CreateRoleGroupCommand, CreateRoleGroupResult>(logger, mediator, requestManagement)
{
    protected override CreateRoleGroupResult CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, string.Empty);
}

public class UpdateRoleGroupIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<UpdateRoleGroupCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<UpdateRoleGroupCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest() => false;
}

public class DeleteRoleGroupIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<DeleteRoleGroupCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<DeleteRoleGroupCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest() => false;
}



public class CreateRoleIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<CreateRoleCommand, CreateRoleResult>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<CreateRoleCommand, CreateRoleResult>(logger, mediator, requestManagement)
{
    protected override CreateRoleResult CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, string.Empty);
}

public class UpdateRoleIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<UpdateRoleCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<UpdateRoleCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest() => false;
}

public class DeleteRoleIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<DeleteRoleCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<DeleteRoleCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest() => false;
}
