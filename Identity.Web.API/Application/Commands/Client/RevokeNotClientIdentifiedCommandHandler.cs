using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands.Client;

public class RevokeNotClientIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<RevokeNotClientCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<RevokeNotClientCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest() => false;
}
