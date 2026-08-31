namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 邮箱自动注册命令（验证码登入未注册邮箱时由 <see cref="LogInCommandHandler"/> 派发）。
/// 验证码注册用户无自定义密码：注册即由 CSPRNG 生成初始密码，经邮件后台队列明文下发到该邮箱，
/// 后续用户可通过修改密码接口设置自有密码完成密码登入。
/// 重复注册由 Email 唯一索引兜底（TOCTOU 竞态由 DB 约束终结）。
/// 注意：调用方必须先校验并一次性消费邮箱验证码再派发本命令。
/// </summary>
public record RegisterByEmailCommand(string Email) : IRequest<RegisterByEmailResult?>, ILoggableCommand
{
    public string IdProperty => nameof(Email);
    public string IdValue => Email;
}

/// <summary>
/// 邮箱自动注册命令结果。
/// </summary>
/// <param name="User">本次注册创建的用户；并发冲突时返回已存在用户</param>
/// <param name="IsNewUser">本次真正创建了新账号时为 true</param>
public record RegisterByEmailResult(User User, bool IsNewUser);