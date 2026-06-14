using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public abstract class IdentifiedCommandHandler<T, R>(
    ILogger<IdentifiedCommandHandler<T, R>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : NotMediator.IRequestHandler<IdentifiedCommand<T, R>, R>
    where T : IRequest<R>
{
    private readonly ILogger<IdentifiedCommandHandler<T, R>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly INotMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly IRequestManagement _requestManagement =
        requestManagement ?? throw new ArgumentNullException(nameof(requestManagement));

    public async Task<R> Handler(IdentifiedCommand<T, R> request, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await _requestManagement.ExecuteAsync(request.Id);
        if (alreadyProcessed)
        {
            return CreateResultForDuplicateRequest();
        }
        else
        {
            await _requestManagement.CreateRequestForCommandAsync<T>(request.Id);
            try
            {
                var command = request.Command;
                var commandName = command.GetGenericTypeName();
                var idProvider = string.Empty;
                var commandId = string.Empty;
                switch (command)
                {
                    case CreateUserCommand createUserCommand:
                        idProvider = nameof(createUserCommand.Email);
                        commandId = createUserCommand.Email;
                        break;
                    case GenerateCodeCommand generateCodeCommand:
                        idProvider = nameof(generateCodeCommand.Email);
                        commandId = generateCodeCommand.Email;
                        break;
                    // case CreateByUserCommand createByUserCommand:
                    //     idProvider = nameof(createByUserCommand.Email);
                    //     commandId = createByUserCommand.Email;
                    //     break;
                    default:
                        idProvider = "id/?";
                        commandId = "?";
                        break;
                }

                _logger.LogInformation(
                    "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                    commandName,
                    idProvider,
                    commandId,
                    command);
                var response = await _mediator.SendAsync(command, cancellationToken);
                _logger.LogInformation("Handled Command {CommandName} {@Command} with response {@Response}:",
                    commandName, command, response);
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error handling command  {@Command}: {Error}",
                    request.Command, e.Message);
                throw;
            }
        }
    }

    protected abstract R CreateResultForDuplicateRequest();
}