using Notcomd.Token.JWT.Security;

namespace Identity.Web.API.Application.Commands;

public class GenerateCodeCommandHandler(
    ILogger<GenerateCodeCommandHandler> logger,
    IEmailCodeSend emailCodeSend)
    : NotMediator.IRequestHandler<GenerateCodeCommand, string>
{
    public async Task<string> Handler(GenerateCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var code = await JwtRandom.CreateRandomStringValueTask();
            logger.LogInformation("[{Time}] 创建验证码: {Code}", DateTime.UtcNow, code);

            await emailCodeSend.SendEmailCodeAsync(request.Email, code);
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