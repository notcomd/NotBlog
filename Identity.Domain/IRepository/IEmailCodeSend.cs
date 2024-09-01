namespace Identity.Domain.IRepository;

public interface IEmailCodeSend
{
    ValueTask SendEmailCodeAsync(string sendEmail, string code);
}