using Identity.Domain.IRepository;

namespace Identity.Infrastructure.Repository;

public class EmailCodeSend:IEmailCodeSend
{
    public ValueTask SendEmailCodeAsync(string sendEmail, string code)
    {
        throw new NotImplementedException();
    }
}