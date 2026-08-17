namespace Message.Web.API.Application.Commands.UserInfo;

/// <summary>更新背景封面命令（不存在则创建用户资料，upsert 语义）。</summary>
public record UpdateBackgroundCoverCommand(Guid UserId, Uri? BackgroundCoverUrl) : IRequest<bool>;
