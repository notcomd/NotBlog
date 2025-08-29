namespace Identity.Domain.IRepository;

public interface IEmailCodeSend
{
    ValueTask SendEmailCodeAsync(string subject, string toEmail, string code);
}