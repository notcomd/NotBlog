using FileDev.Infrastructure.Idempotent;
using Notcomd.Evenbus;
using NotMediator;

namespace FileDev.Web.API.ActionFilter.Command;

public abstract class IdentifiedCommandHandler<T, R>(
    INotMediator mediator,
    IRequestManagement requestManagement,
    ILogger<IdentifiedCommandHandler<T, R>> logger)
    : IRequestHandler<IdentifiedCommand<T, R>, R> where T : IRequest<R>
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
                    case CreateNotFileCommand createNotFileCommand:
                        idProvider = nameof(createNotFileCommand.UserGuid);
                        commandId = createNotFileCommand.UserGuid.ToString();
                        break;
                    case CreateNotFileGroupCommand createNotFileGroupCommand:
                        idProvider = "Guid";
                        commandId = "";
                        break;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling command  {@Command}: {Error}",
                    request.Command, ex.Message);
                throw;
            }
        }
    }

    protected abstract R CreateResultForDuplicateRequest();
}