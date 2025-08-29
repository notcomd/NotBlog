namespace Identity.Web.API.Application.Command;

public class SendWithEmailCommandHandler:IRequestHandler<SendWithEmailCommand, bool>
{

    private readonly IEmailCodeSend _emailCodeSend;
    private readonly ILogger<SendWithEmailCommandHandler> _logger;
    private readonly INotDateTime _notDateTime;

    public SendWithEmailCommandHandler(IEmailCodeSend emailCodeSend, ILogger<SendWithEmailCommandHandler> logger, 
        INotDateTime notDateTime)
    {
        _emailCodeSend = emailCodeSend;
        _logger = logger;
        _notDateTime = notDateTime;
    }

    public async Task<bool> Handler(SendWithEmailCommand request, CancellationToken cancellationToken)
    {
        await _emailCodeSend.SendEmailCodeAsync(request.Subject,request.ToEmailAddress, request.GeneratedCode);
        return true;
    }


}

