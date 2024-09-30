using EmailSendServer;
using Identity.Domain.IRepository;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repository;

public class EmailCodeSend : IEmailCodeSend
{
    private readonly IEmail _email;
    private readonly ILogger<IEmail> _logger;

    public EmailCodeSend(IEmail email, ILogger<IEmail> logger)
    {
        _email = email;
        _logger = logger;
    }
    
    public ValueTask SendEmailCodeAsync(string sendEmail, string code)
    {
        return ValueTask.CompletedTask;
    }
}