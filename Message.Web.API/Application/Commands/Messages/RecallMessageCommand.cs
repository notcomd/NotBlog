using Message.Infrastructure.Services;

namespace Message.Web.API.Application.Commands.Messages;

/// <summary>
/// 撤回消息命令。
/// </summary>
/// <param name="MessageId">消息 ID</param>
/// <param name="UserId">撤回者用户 ID</param>
/// <param name="Reason">撤回原因</param>
public record RecallMessageCommand(Guid MessageId, Guid UserId, RecallReason Reason) : IRequest<bool>;

/// <summary>
/// 撤回消息命令处理程序。
/// </summary>
public class RecallMessageCommandHandler(
    IMessageRepository messageRepository,
    ILogger<RecallMessageCommandHandler> logger,
    RedisCacheService redisCache) : IRequestHandler<RecallMessageCommand, bool>
{
    public async Task<bool> Handler(RecallMessageCommand command, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(command.MessageId);
        if (message == null)
            throw new KeyNotFoundException("消息不存在");

        message.Recall(command.UserId, command.Reason, null);
        await messageRepository.UpdateAsync(message);
        await messageRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Q-05：撤回改变消息状态，失效发送者/接收者的消息详情缓存（Key 带用户维度，与 MessagesApi.GetMessageAsync 一致）
        await redisCache.RemoveAsync(MessageCacheKey(message.SenderId, command.MessageId), cancellationToken);
        if (message.ReceiverId is { } receiverId)
            await redisCache.RemoveAsync(MessageCacheKey(receiverId, command.MessageId), cancellationToken);

        logger.LogInformation("消息 {MessageId} 已撤回，原因={Reason}", command.MessageId, command.Reason);
        return true;
    }

    /// <summary>消息详情缓存 Key（带用户维度，与 MessagesApi.GetMessageAsync 保持一致）</summary>
    private static string MessageCacheKey(Guid userId, Guid messageId) => $"message:msg:{userId}:{messageId}";
}
