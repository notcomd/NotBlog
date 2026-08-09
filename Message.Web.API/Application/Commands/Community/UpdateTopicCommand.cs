namespace Message.Web.API.Application.Commands.Community;

/// <summary>更新话题命令（R-12：仅创建者或管理员）。</summary>
public record UpdateTopicCommand(Guid TopicGuid, string Name, string? Description) : IRequest<bool>;
