using CacheMemory.Core;
using Identity.Domain.ICache;
using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class GenerateCodeCommandHandler(
    ILogger<GenerateCodeCommandHandler> logger,
    IEmailCodeSend emailCodeSend,
    IIdentityCacheService identityCacheService)
    : IRequestHandler<GenerateCodeCommand, string>
{
    public async Task<string> Handler(GenerateCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var code = await JwtRandom.CreateRandomStringValueTask();

            // 验证码 5 分钟后过期，避免永久驻留 Redis 被暴力破解
            await identityCacheService.SetStringAsync($"Login_{request.Email}", code,
                TimeSpan.FromMinutes(5), cancellationToken);

            // S-16：验证码不得写入日志
            await emailCodeSend.SendEmailCodeAsync(request.Email,  "登录验证码", code);

            logger.LogInformation("[{Time}] 验证码已发送至: {Email}", DateTime.UtcNow, request.Email);

            return code;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Time}] 发送验证码失败: {Email}", DateTime.UtcNow, request.Email);
            throw;
        }
    }
}

public class GenerateCodeIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<GenerateCodeCommand, string>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<GenerateCodeCommand, string>(logger, mediator, requestManagement)
{
    protected override string CreateResultForDuplicateRequest()
    {
        return string.Empty;
    }
}