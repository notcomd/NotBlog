namespace Identity.Web.API.Application.Commands;

public class RegisterByUserCommandHandler(
    ILogger<RegisterByUserCommandHandler> logger,
    IUserService userService
)
    : NotMediator.IRequestHandler<RegisterByUserCommand, bool>
{
    public async Task<bool> Handler(RegisterByUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var data = await userService.GetUserByEmailAsync(command.UserEmail);
            if (data != null)
            {
                logger.LogWarning("[{Time}] 用户邮箱已注册，无法重复注册: {Email}", DateTime.UtcNow, command.UserEmail);
                return false;
            }

            if (command.PasswordHash.Length < 6)
            {
                logger.LogWarning("[{Time}] 用户密码长度不能小于6位: {Email}", DateTime.UtcNow, command.UserEmail);
                return false;
            }

            if (command.Code != command.Code)
            {
                logger.LogWarning("[{Time}] 用户验证码与确认验证码不一致: {Email}", DateTime.UtcNow, command.UserEmail);
                return false;
            }

            var result =
                await userService.RegisterByCreateUserAsync(command.UserEmail, command.PasswordHash, command.Code);
            if (!result)
            {
                logger.LogWarning("[{Time}] 用户注册失败: {Email}", DateTime.UtcNow, command.UserEmail);
                return false;
            }

            logger.LogInformation("[{Time}] 用户注册: {Email}, 结果: {Result}", DateTime.UtcNow, command.UserEmail, result);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Time}] 用户注册失败: {Email}", DateTime.UtcNow, command.UserEmail);
            return false;
        }
    }
}