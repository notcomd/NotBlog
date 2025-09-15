
<<<<<<< HEAD
using Identity.Domain.IdentiyResult;

=======
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
namespace Identity.Web.API.Application.Command;

public class ChangeByUserCommandHandler : IRequestHandler<ChangeByUserCommand, bool>
{

    private readonly INotDateTime _notDateTime;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ChangeByUserCommandHandler> _logger;
    private readonly IdentityDomainUserManagerServer _identityDomainUserManagerServer;

    public ChangeByUserCommandHandler(INotDateTime notDateTime, IUserRepository userRepository, ILogger<ChangeByUserCommandHandler> logger, IdentityDomainUserManagerServer identityDomainUserManagerServer)
    {
        _notDateTime = notDateTime;
        _userRepository = userRepository;
        _logger = logger;
        _identityDomainUserManagerServer = identityDomainUserManagerServer;
    }

    public async Task<bool> Handler(ChangeByUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
<<<<<<< HEAD
            if (request is null) throw new ArgumentNullException(nameof(request));
            var changeByUserDto = new ChangeByUserDto(request.UserName, request.Address, request.ImageCoverUri);
            if (await _identityDomainUserManagerServer.ChangeWithEmailUserAsync(request.Email, changeByUserDto) != UserAccessResult.Success)
            {
                _logger?.LogWarning($"[(≧ ﹏ ≦){_notDateTime?.UtcNow}]ChangeByUserCommand Failed! {request.Email}");
                return false;
            }
            await _userRepository.UnitOfWork.SavaChangesAsync(cancellationToken);
=======
            if(request is null) throw new ArgumentNullException(nameof(request));
            var changeByUserDto=new ChangeByUserDto(request.UserName,request.Address,request.ImageCoverUri);
            await _identityDomainUserManagerServer.ChangeWithEmailUserAsync(request.Email,changeByUserDto);
            await _userRepository.UnitOfWork.SavaChangesAsync();
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
            _logger?.LogInformation($"[（*＾-＾*）{_notDateTime?.UtcNow}]ChangeByUserCommand Success! {request.Email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime?.UtcNow}]ChangeByUserCommand Failed! {request.Email}");
<<<<<<< HEAD
            return false;
=======
            throw;
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
        }
    }
}
