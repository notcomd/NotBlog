using NotMediator;
using Notcomd.EventBus.Core;
namespace FileDev.Web.API.Application.Command;

public abstract class IdentifiedCommandHandler<T, R>(
    INotMediator mediator,
    IRequestManagement requestManagement,
    ILogger<IdentifiedCommandHandler<T, R>> logger)
    : NotMediator.IRequestHandler<IdentifiedCommand<T, R>, R> where T : IRequest<R>
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
                        // Major：原代码 idProvider 写死 "Guid"、commandId 为空字符串，无法定位具体文件组。
                        // 改为记录 UserGuid 与 FileGroupName，便于审计追踪。
                        idProvider = nameof(createNotFileGroupCommand.UserGuid);
                        commandId = createNotFileGroupCommand.UserGuid.ToString();
                        break;
                    default:
                        idProvider = "id/?";
                        commandId = "?";
                        break;
                }

                _logger.LogInformation(
                    "Sending command: {CommandName} - {IdProperty}: {CommandId}",
                    commandName,
                    idProvider,
                    commandId);

                var response = await _mediator.SendAsync(command, cancellationToken);

                _logger.LogInformation("Handled Command {CommandName}", commandName);

                return response;
            }
            catch (Exception ex)
            {
                // Major：异常日志记录 ex.Message 可能泄露内部信息，但服务端日志可接受
                _logger.LogError(ex, "Error handling command {CommandName}",
                    request.Command.GetGenericTypeName());
                throw;
            }
        }
    }

    protected abstract R CreateResultForDuplicateRequest();
}