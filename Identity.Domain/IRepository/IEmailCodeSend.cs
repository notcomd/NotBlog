namespace Identity.Domain.IRepository;

public interface IEmailCodeSend
{
    ValueTask SendEmailCodeAsync(string toEmail, string code);
}