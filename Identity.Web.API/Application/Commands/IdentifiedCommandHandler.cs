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

        await _requestManagement.CreateRequestForCommandAsync<T>(request.Id);
        try
        {
            var command = request.Command;
            var commandName = command.GetGenericTypeName();
            var (idProvider, commandId) = command is ILoggableCommand loggable
                ? (loggable.IdProperty, loggable.IdValue)
                : ("unknown", "?");

            // S-16：不记录命令对象（可能含明文密码/验证码），仅记录命令名与标识
            _logger.LogInformation(
                "Sending command: {CommandName} - {IdProperty}: {CommandId}",
                commandName, idProvider, commandId);
            var response = await _mediator.SendAsync(command, cancellationToken);
            _logger.LogInformation("Handled Command {CommandName} with response type {ResponseType}",
                commandName, response?.GetType().Name);
            return response;
        }
        catch (Exception e)
        {
            // S-16：不记录命令对象（可能含明文密码/验证码），仅记录异常信息
            _logger.LogError(e, "Error handling command {CommandName}: {Error}",
                request.Command.GetGenericTypeName(), e.Message);

            // S-14：命令执行失败时回滚幂等记录，允许客户端使用同一幂等键重试
            try
            {
                await _requestManagement.RemoveRequestAsync(request.Id);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogWarning(rollbackEx, "回滚幂等记录失败: {IdempotencyKey}", request.Id);
            }

            throw;
        }
    }

    protected abstract R CreateResultForDuplicateRequest();
}