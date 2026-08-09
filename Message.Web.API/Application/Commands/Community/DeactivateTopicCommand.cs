namespace Message.Web.API.Application.Commands.Community;

/// <summary>停用话题命令（R-12：仅创建者或管理员；停用后列表与帖子流不再展示）。</summary>
public record DeactivateTopicCommand(Guid TopicGuid) : IRequest<bool>;
