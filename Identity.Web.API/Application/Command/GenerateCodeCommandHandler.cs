using System.Text;

namespace Identity.Web.API.Application.Command;

public class GenerateCodeCommandHandler : IRequestHandler<GenerateCodeCommand, bool>
{

    private readonly IEmail _email;
    private readonly IJwtTokenService _jwtTokenServer;
    private readonly INotMediator _notMediator;
    private readonly ILogger<GenerateCodeCommandHandler> _logger;
    private readonly INotMemoryCache _notMemoryCache;
    private readonly INotDateTime _notDateTime;

    public GenerateCodeCommandHandler(IEmail email, INotMediator notMediator, IJwtTokenService jwtTokenServer, ILogger<GenerateCodeCommandHandler> logger = null, INotMemoryCache notMemoryCache = null, INotDateTime notDateTime = null)
    {
        _email = email ?? throw new ArgumentNullException(nameof(email));
        _jwtTokenServer = jwtTokenServer ?? throw new ArgumentNullException(nameof(jwtTokenServer));
        _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));
        _logger = logger;
        _notMemoryCache = notMemoryCache;
        _notDateTime = notDateTime;
    }

    public async Task<bool> Handler(GenerateCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);            
            await _notMemoryCache.AddByMemoryCacheAsync($"{request.MemoryKey}{request.Email}", request.GenerateCode);
            _logger?.LogInformation($"[（*＾-＾*）{_notDateTime?.UtcNow}]Generate Code Success! {request.Email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime?.UtcNow}]Generate Code Failed! {request.Email}");
            return false;
        }
    }
}