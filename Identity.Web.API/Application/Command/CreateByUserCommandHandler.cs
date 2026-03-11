namespace Identity.Web.API.Application.Command;

public class CreateByUserCommandHandler : IRequestHandler<CreateByUserCommand, bool>
{
    private readonly IEmailCodeSend _emailCodeSend;


    private readonly ILogger<CreateByUserCommandHandler> _logger;

    public CreateByUserCommandHandler(ILogger<CreateByUserCommandHandler> logger, IEmailCodeSend emailCodeSend)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailCodeSend = emailCodeSend ?? throw new ArgumentNullException(nameof(emailCodeSend));
    }

    public async Task<bool> Handler(CreateByUserCommand request, CancellationToken cancellationToken)
    {
        await _emailCodeSend.SendEmailCodeAsync(request.Email, request.Code);
        _logger.LogInformation($"[{DateTime.UtcNow}]Email Send! ");
        return true;
    }
}