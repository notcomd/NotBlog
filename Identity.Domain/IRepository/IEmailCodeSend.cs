namespace Identity.Domain.IRepository;

public interface IEmailCodeSend
{
    Task<bool> SendEmailCodeAsync(string toEmail, string subject, string body);
}