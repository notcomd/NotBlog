namespace Message.Web.API.Application.Commands.UserInfo;

/// <summary>增加硬币命令（不存在则创建用户资料，upsert 语义）。</summary>
public record AddUserCoinsCommand(Guid UserId, long Amount) : IRequest<bool>;
