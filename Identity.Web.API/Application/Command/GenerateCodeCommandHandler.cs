namespace Identity.Web.API.Application.Command;

public class GenerateCodeCommandHandler(IEmail email, INotMediator notMediator, IJwtTokenService jwtTokenServer)
    : IRequestHandler<GenerateCodeCommand, string>
{
    private readonly IEmail _email = email ?? throw new ArgumentNullException(nameof(email));

    private readonly IJwtTokenService _jwtTokenServer =
        jwtTokenServer ?? throw new ArgumentNullException(nameof(jwtTokenServer));


    private readonly INotMediator _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));

    public Task<string> Handler(GenerateCodeCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}