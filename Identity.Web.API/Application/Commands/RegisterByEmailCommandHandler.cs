namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 邮箱自动注册命令处理器：
/// 1. 取默认角色 "User"（未配置则抛异常，拒绝无角色注册）；
/// 2. CSPRNG 生成初始密码并以 <see cref="User.CreateByEmailUser"/> 创建账号（保存时领域事件触发生成欢迎邮件）；
/// 3. 落库：并发同邮箱注册由 Email 唯一索引兜底，冲突时改取已存在用户继续登入；
/// 4. 初始密码经邮件后台队列明文下发（P6：不入数据库事务内做 SMTP 外部 IO）。
/// </summary>
public class RegisterByEmailCommandHandler(
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IMailQueue mailQueue,
    ILogger<RegisterByEmailCommandHandler> logger)
    : IRequestHandler<RegisterByEmailCommand, RegisterByEmailResult?>
{
    public async Task<RegisterByEmailResult?> Handler(RegisterByEmailCommand request, CancellationToken cancellationToken)
    {
        var userRole = await userRoleRepository.FindByUserRoleAsync("User");
        if (userRole is null)
        {
            logger.LogError("[{DateTime}] 默认角色 'User' 未配置，无法自动注册: {Email}", DateTime.UtcNow, request.Email);
            throw new InvalidOperationException("默认角色 'User' 未在数据库中配置。");
        }

        var initialPassword = JwtRandom.GenerateComplexPassword();
        var user = await User.CreateByEmailUser(
            userRole.RoleGuid, request.Email, initialPassword, null, null);
        await userRepository.AddOneByUserAsync(user);

        try
        {
            await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // 并发登录同一陌生邮箱同时自动注册：唯一索引兜底，取已存在用户继续登入
            logger.LogWarning("[{DateTime}] 并发自动注册冲突，邮箱已存在: {Email}", DateTime.UtcNow, request.Email);
            var existing = await userRepository.FindOneByUserAsync(request.Email);
            return existing is null ? null : new RegisterByEmailResult(existing, false);
        }

        // P6：初始密码经邮件后台队列发送，不占用数据库事务做 SMTP 外部 IO
        mailQueue.Enqueue(request.Email, "账号开通通知",
            $"您已通过邮箱验证码成功创建账号。您的初始密码为：{initialPassword}，请登录后尽快修改密码。");

        logger.LogInformation("[{DateTime}] 邮箱自动注册成功并已下发初始密码: {Email}", DateTime.UtcNow, request.Email);
        return new RegisterByEmailResult(user, true);
    }
}