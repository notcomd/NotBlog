using System.Text;

using Identity.Domain.Server;

namespace Identity.Web.API.Application.Command;

public class GenerateCodeCommandHandler : IRequestHandler<GenerateCodeCommand, string>
{

    private readonly IEmail _email;
    private readonly IJwtTokenService _jwtTokenServer;
    private readonly INotMediator _notMediator;
    private readonly ILogger<GenerateCodeCommandHandler> _logger;
    private readonly IdentityDomainToolServer _identityDomainToolServer;
    private readonly INotDateTime _notDateTime;

    public GenerateCodeCommandHandler(IEmail email, INotMediator notMediator, IJwtTokenService jwtTokenServer,
        ILogger<GenerateCodeCommandHandler> logger, INotDateTime notDateTime, IdentityDomainToolServer identityDomainToolServer)
    {
        _email = email ?? throw new ArgumentNullException(nameof(email));
        _jwtTokenServer = jwtTokenServer ?? throw new ArgumentNullException(nameof(jwtTokenServer));
        _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _notDateTime = notDateTime??throw new ArgumentNullException(nameof(notDateTime));
        _identityDomainToolServer = identityDomainToolServer?? throw new ArgumentNullException(nameof(identityDomainToolServer));
    }

    public async Task<string> Handler(GenerateCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);            
            await _identityDomainToolServer.GenerateWithCodeAsync(request.Email, request.LenghtGenerate);
            var code= await _identityDomainToolServer.GetCodeByMemoryCacheAsync(request.Email);
            _logger?.LogInformation($"[（*＾-＾*）{_notDateTime?.UtcNow}]Generate Code Success! {request.Email}");
            return code;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime?.UtcNow}]Generate Code Failed! {request.Email}");
            throw;
        }
    }
}