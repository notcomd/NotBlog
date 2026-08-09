namespace Message.Web.API.Application.Commands.Community;

/// <summary>停用话题命令处理程序（R-12：创建者本人或管理员可操作）。</summary>
public class DeactivateTopicCommandHandler(
    ITopicRepository topicRepository,
    ICurrentUserService currentUser,
    ILogger<DeactivateTopicCommandHandler> logger) : IRequestHandler<DeactivateTopicCommand, bool>
{
    public async Task<bool> Handler(DeactivateTopicCommand command, CancellationToken cancellationToken)
    {
        var topic = await topicRepository.GetByIdAsync(command.TopicGuid);
        if (topic == null)
            throw new KeyNotFoundException("话题不存在");

        // 权限：创建者本人或管理员
        var operatorId = currentUser.GetUserId();
        if (topic.CreatorGuid != operatorId && !currentUser.IsAdmin())
            throw new UnauthorizedAccessException("仅话题创建者或管理员可停用话题");

        if (topic.IsActive)
        {
            topic.Deactivate();
            await topicRepository.UpdateAsync(topic);
            await topicRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        logger.LogInformation("话题 {TopicGuid} 已停用，操作者={OperatorId}", command.TopicGuid, operatorId);
        return true;
    }
}
