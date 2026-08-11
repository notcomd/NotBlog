namespace Message.Web.API.Application.Commands.UserInfo;

/// <summary>每日签到命令（固定 +250 经验，每日一次）。</summary>
public record SignInCommand(Guid UserId) : IRequest<SignInResultDto>;
