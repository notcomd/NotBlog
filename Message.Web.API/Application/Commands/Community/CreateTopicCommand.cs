namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 创建话题命令。
/// </summary>
public record CreateTopicCommand(Guid CreatorGuid, string Name, string? Description) : IRequest<Guid>;
