namespace Identity.Web.API.Application.CommandHandler;

public class GenerateCodeCommandHandler : IRequestHandler<GenerateCodeCommand, string>
{

    public Task<string> Handler(GenerateCodeCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}