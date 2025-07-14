namespace Identity.Web.API.Application.Command;

public class GenerateCodeCommandHandler : IRequestHandler<GenerateCodeCommand, string>
{

    private readonly IEmail _email;
    private readonly IJwtTokenService _jwtTokenServer;
    private readonly INotMediator _notMediator;

    public GenerateCodeCommandHandler(IEmail email, INotMediator notMediator, IJwtTokenService jwtTokenServer)
    {
        _email = email ?? throw new ArgumentNullException(nameof(email));
        _jwtTokenServer = jwtTokenServer ?? throw new ArgumentNullException(nameof(jwtTokenServer));
        _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));
    }

    public Task<string> Handler(GenerateCodeCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}