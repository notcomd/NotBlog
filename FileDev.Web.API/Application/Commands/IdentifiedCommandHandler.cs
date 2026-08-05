using System.Diagnostics;
using NotMediator;
using Notcomd.EventBus.Core;
namespace FileDev.Web.API.Application.Commands;

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
        // S-16：不记录命令对象（可能含敏感内容），仅记录命令类型与幂等键
        _logger.LogInformation(
            "Begin handling idempotent command: CommandType={CommandType}, IdempotencyKey={IdempotencyKey}",
            typeof(T).Name, request.Id);

        // 幂等检查（快速路径）：请求已处理过则直接返回重复结果
        var alreadyProcessed = await _requestManagement.ExecuteAsync(request.Id);
        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Idempotency check hit, request already processed: IdempotencyKey={IdempotencyKey}",
                request.Id);
            return CreateResultForDuplicateRequest();
        }

        // insert-or-detect：并发下相同 request 仅一个成功插入，其余返回 false 视为重复，
        // 从根上消除「先查后插」的 TOCTOU 竞态窗口（避免命令被重复执行）
        var created = await _requestManagement.CreateRequestForCommandAsync<T>(request.Id);
        if (!created)
        {
            _logger.LogInformation(
                "Idempotency record already created by concurrent request: IdempotencyKey={IdempotencyKey}",
                request.Id);
            return CreateResultForDuplicateRequest();
        }

        var startTimestamp = Stopwatch.GetTimestamp();
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
                    idProvider = nameof(createNotFileGroupCommand.UserGuid);
                    commandId = createNotFileGroupCommand.UserGuid.ToString();
                    break;
                default:
                    idProvider = "id/?";
                    commandId = "?";
                    break;
            }

            _logger.LogInformation(
                "Sending command: CommandName={CommandName}, IdProperty={IdProperty}, CommandId={CommandId}, IdempotencyKey={IdempotencyKey}",
                commandName,
                idProvider,
                commandId,
                request.Id);

            var response = await _mediator.SendAsync(command, cancellationToken);

            _logger.LogInformation(
                "Command handled successfully: CommandName={CommandName}, IdempotencyKey={IdempotencyKey}, ElapsedMs={ElapsedMs:0.00}",
                commandName, request.Id,
                Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling command: CommandName={CommandName}, IdempotencyKey={IdempotencyKey}, ElapsedMs={ElapsedMs:0.00}",
                request.Command.GetGenericTypeName(), request.Id,
                Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);

            // S-14：命令执行失败时回滚幂等记录，允许客户端使用同一幂等键重试
            try
            {
                await _requestManagement.RemoveRequestAsync(request.Id);
                _logger.LogInformation(
                    "Idempotency record rolled back: IdempotencyKey={IdempotencyKey}", request.Id);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogWarning(rollbackEx,
                    "Failed to rollback idempotency record: IdempotencyKey={IdempotencyKey}", request.Id);
            }

            throw;
        }
    }

    protected abstract R CreateResultForDuplicateRequest();
}