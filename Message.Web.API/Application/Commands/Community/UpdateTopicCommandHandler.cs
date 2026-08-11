namespace Message.Web.API.Application.Commands.Community;

/// <summary>更新话题命令处理程序（R-12：创建者本人或管理员可操作；名称唯一性校验排除自身）。</summary>
public class UpdateTopicCommandHandler(
    ITopicRepository topicRepository,
    ICurrentUserService currentUser,
    ILogger<UpdateTopicCommandHandler> logger) : IRequestHandler<UpdateTopicCommand, bool>
{
    public async Task<bool> Handler(UpdateTopicCommand command, CancellationToken cancellationToken)
    {
        var topic = await topicRepository.GetByIdAsync(command.TopicGuid);
        if (topic == null)
            throw new KeyNotFoundException("话题不存在");

        // 权限：创建者本人或管理员
        var operatorId = currentUser.GetUserId();
        if (topic.CreatorGuid != operatorId && !currentUser.IsAdmin())
            throw new UnauthorizedAccessException("仅话题创建者或管理员可修改话题");

        var trimmed = command.Name.Trim();

        // 名称唯一性（大小写不敏感；排除自身）
        if (!string.Equals(topic.Name, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await topicRepository.GetByNameAsync(trimmed);
            if (existing is not null && existing.TopicGuid != topic.TopicGuid)
                throw new InvalidOperationException("话题名称已存在");
        }

        topic.UpdateInfo(trimmed, command.Description);
        await topicRepository.UpdateAsync(topic);
        await topicRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("话题 {TopicGuid} 已更新为「{Name}」", command.TopicGuid, trimmed);
        return true;
    }
}
