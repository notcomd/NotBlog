using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands.Client;

public class CreateNotClientIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<CreateNotClientCommand, CreateNotClientResult>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<CreateNotClientCommand, CreateNotClientResult>(logger, mediator, requestManagement)
{
    protected override CreateNotClientResult CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, string.Empty);
}
