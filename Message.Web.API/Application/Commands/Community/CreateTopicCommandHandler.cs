
namespace Message.Web.API.Application.Commands.Community;
/// <summary>创建话题命令处理程序。</summary>
public class CreateTopicCommandHandler(
    ITopicRepository topicRepository,
    ILogger<CreateTopicCommandHandler> logger) : IRequestHandler<CreateTopicCommand, Guid>
{
    public async Task<Guid> Handler(CreateTopicCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始创建话题: {Name}", command.Name);

            if (await topicRepository.NameExistsAsync(command.Name.Trim()))
                throw new InvalidOperationException("话题已存在");

            var topic = Topic.Create(command.Name, command.Description, command.CreatorGuid);

            await topicRepository.AddAsync(topic);
            await topicRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("话题创建成功: {TopicGuid}", topic.TopicGuid);
            return topic.TopicGuid;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            logger.LogError(ex, "创建话题失败: {Name}", command.Name);
            throw;
        }
    }
}
