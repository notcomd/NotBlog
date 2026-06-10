using Identity.Domain.IService;

namespace Identity.Web.API.Application.Commands;

public class CreateUserCommandHandler(
    ILogger<CreateUserCommandHandler> logger,
    IUserService userService)
    : IRequestHandler<CreateUserCommand, bool>
{
    public async Task<bool> Handler(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await userService.SignInByCreateUserAsync(request.Email, request.Password, request.Code);
            logger.LogInformation("[{Time}] 用户注册: {Email}, 结果: {Result}", DateTime.UtcNow, request.Email, result);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Time}] 用户注册失败: {Email}", DateTime.UtcNow, request.Email);
            return false;
        }
    }
}