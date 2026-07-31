using Notcomd.NotEmail.Core;

namespace Identity.Infrastructure.Repository;

public class EmailCodeSend(IEmailManager emailManager) : IEmailCodeSend
{
    public async Task<bool> SendEmailCodeAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrEmpty(toEmail) && string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(body))
        {
            throw new ArgumentNullException($"{toEmail} and {subject} and {body} is null");
        }
        var result = await emailManager.SendAsync(new EmailMessage(toEmail, subject, body, isHtml: false));
        return result.Success;
    }
}