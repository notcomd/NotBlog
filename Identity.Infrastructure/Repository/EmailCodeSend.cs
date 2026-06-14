namespace Identity.Infrastructure.Repository;

public class EmailCodeSend : IEmailCodeSend
{
    public async ValueTask SendEmailCodeAsync(string toEmail, string code)
    {
        throw new NotImplementedException();
    }
}