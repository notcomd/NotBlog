using System.Security.Cryptography.Xml;

using Identity.Domain.IdentiyResult;


namespace Identity.Web.API.Application.Command;

public class CreateByEmailUserCommandHandler : IRequestHandler<CreateByEmailUserCommand, bool>
{


    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly INotDateTime _notDateTime;
    private readonly ILogger<CreateByEmailUserCommandHandler> _logger;
    private readonly INotMediator _notMediator;
    private readonly IdentityDomainRegisterServer _identityDomainSignUpServer;



    public CreateByEmailUserCommandHandler(IUserRepository userRepository, IUserRoleRepository userRoleRepository,
        ILogger<CreateByEmailUserCommandHandler> logger, INotDateTime notDateTime,
        IdentityDomainRegisterServer identityDomainSignUpServer, INotMediator notMediator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notDateTime = notDateTime ?? throw new ArgumentNullException(nameof(notDateTime));
        _identityDomainSignUpServer = identityDomainSignUpServer ?? throw new ArgumentNullException(nameof(identityDomainSignUpServer));
        _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notDateTime));
    }



    public async Task<bool> Handler(CreateByEmailUserCommand request, CancellationToken cancellationToken)
    {

        try
        {                     
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            if(await _identityDomainSignUpServer.RegisterWhitEmailAsync(request.Email, request.Password,request.RoleName) is not UserAccessResult.Success)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){DateTime.UtcNow}]User Already Exists! {request.Email}");
                return false;
            }
            await _userRoleRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);          
           
            _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]User Created! {request.Email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){DateTime.UtcNow}]User Create Failed! {request.Email}");
            return false;
        }

    }

    public class CreateByUserIdentifiedCommandHandler : IdentifiedCommandHandler<CreateByEmailUserCommand, bool>
    {
        public CreateByUserIdentifiedCommandHandler(IRequestManager requestManager,
        ILogger<CreateByUserIdentifiedCommandHandler> logger,
        INotMediator notMediator) : base(notMediator, requestManager, logger)
        {
        }
        protected override bool CreateResultForDuplicateRequest()
        {
            return true;
        }
    }
}