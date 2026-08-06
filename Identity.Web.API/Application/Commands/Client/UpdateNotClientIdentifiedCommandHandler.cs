using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands.Client;

public class UpdateNotClientIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<UpdateNotClientCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<UpdateNotClientCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest() => false;
}
