namespace Identity.Web.API.Application.Command;

public class CreateByPhoneUserCommandHandler : IRequestHandler<CreateByPhoneUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<CreateByPhoneUserCommandHandler> _logger;
    private readonly INotDateTime _notDateTime;
    private readonly IdentityDomainSignUpServer _identityDomainSignUpServer;
    private readonly INotMediator _notMediator;

    public CreateByPhoneUserCommandHandler(IUserRepository userRepository, IUserRoleRepository userRoleRepository, ILogger<CreateByPhoneUserCommandHandler> logger, INotDateTime notDateTime, IdentityDomainSignUpServer identityDomainSignUpServer, INotMediator notMediator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notDateTime = notDateTime;
        _identityDomainSignUpServer = identityDomainSignUpServer;
        _notMediator = notMediator;
    }

    public async Task<bool> Handler(CreateByPhoneUserCommand request, CancellationToken cancellationToken)
    {
        // _logger.LogInformation($"[{DateTime.UtcNow}]Phone User Created! Phone: {request.PhoneNumber}, Password: {request.Password}");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            await _identityDomainSignUpServer.SigUpWhitPhoneAsync(request.PhoneNumber, request.Password, request.RoleName);
            //await _notMediator.SendAsync(new GenerateCodeCommand(request.PhoneNumber, 9), cancellationToken);
            _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]User Created! {request.PhoneNumber}");
            await _userRoleRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){DateTime.UtcNow}]User Create Failed! {request.PhoneNumber}");
            return false;

        }


    }
}
