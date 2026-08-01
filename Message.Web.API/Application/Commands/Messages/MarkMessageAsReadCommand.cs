namespace Message.Web.API.Application.Commands.Messages;

/// <summary>
/// 标记消息为已读命令。
/// </summary>
/// <param name="MessageId">消息 ID</param>
/// <param name="UserId">阅读者用户 ID</param>
public record MarkMessageAsReadCommand(Guid MessageId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 标记消息为已读命令处理程序。
/// </summary>
public class MarkMessageAsReadCommandHandler(
    IMessageRepository messageRepository,
    ILogger<MarkMessageAsReadCommandHandler> logger) : IRequestHandler<MarkMessageAsReadCommand, bool>
{
    public async Task<bool> Handler(MarkMessageAsReadCommand command, CancellationToken cancellationToken)
    {
        await messageRepository.MarkAsReadAsync(command.MessageId, command.UserId);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        logger.LogInformation("消息 {MessageId} 已标记为已读，用户={UserId}", command.MessageId, command.UserId);
        return true;
    }
}
