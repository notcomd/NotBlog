namespace Message.Web.API.Application.Commands.UserInfo;

/// <summary>扣除硬币命令（余额不足抛业务异常，API 层映射 400）。</summary>
public record ConsumeUserCoinsCommand(Guid UserId, long Amount) : IRequest<bool>;
