using Notcomd.NotEmail.Core;

namespace Identity.Infrastructure.Repository;

public class EmailCodeSend(IEmailManager emailManager, IMemoryRepository<string> memoryRepository) : IEmailCodeSend
{
    public async ValueTask SendEmailCodeAsync(string toEmail, string code)
    {
        if (string.IsNullOrEmpty(toEmail) && string.IsNullOrEmpty(code))
        {
            throw new ArgumentNullException($"{toEmail} and {code} is null");
        }

        _ = await emailManager.SendAsync(new EmailMessage(toEmail, "账号注册",
            $"这是注册账号的验证码，请好好使用不要丢失哦（*＾-＾*）验证码为：[{code}]"));
    }
}