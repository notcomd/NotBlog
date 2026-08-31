namespace Message.Web.API.Application.Commands.UserInfo;

/// <summary>更新个人签名命令（不存在则创建用户资料，upsert 语义；空白视为清除）。</summary>
public record UpdateUserBioCommand(Guid UserId, string? Bio) : IRequest<bool>;