using System.Security.Cryptography.Xml;

using Identity.Domain.IdentiyResult;
using Identity.Domain.Server;

namespace Identity.Web.API.Application.Command;

public class CreateByEmailUserCommandHandler : IRequestHandler<CreateByEmailUserCommand, bool>
{


    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly INotDateTime _notDateTime;
    private readonly ILogger<CreateByEmailUserCommandHandler> _logger;
    private readonly INotMediator _notMediator;
    private readonly IdentityDomainSignUpServer _identityDomainSignUpServer;



    public CreateByEmailUserCommandHandler(IUserRepository userRepository, IUserRoleRepository userRoleRepository,
        ILogger<CreateByEmailUserCommandHandler> logger, INotDateTime notDateTime,
        IdentityDomainSignUpServer identityDomainSignUpServer, INotMediator notMediator)
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
            //ArgumentNullException.ThrowIfNull(request);
           
            //var userData = await _userRepository.FindOneByUserAsync(request.Email);
            //var roleData = await _userRoleRepository.FindByUserRoleAsync(request.RoleName);

            //if (userData is not null || roleData is  null)
            //{
            //    _logger.LogWarning($"[(≧ ﹏ ≦){DateTime.UtcNow}]User Already Exists! {request.Email}");
            //    return false;
            //}

            //var salt = await HashHelper.GenerateSaltValueTask();
            //var stamp = await HashHelper.GenerateSecurityStamp();
            //var passwordHash = await HashHelper.CreateHash256Async(request.Password, salt);
            //var userTrc = new User(roleData.Id, request.Email, passwordHash, _notDateTime.NowOffset);
            //userTrc.UserSafety.ChangeByBlackOrWhiteStatus(EnumBlackOrWhite.AuthorityWhite);
            //userTrc.UserSafety.ChangeByUserStatus(EnumUserStatus.UnActive);

            //if (userTrc is null)
            //{
            //    _logger.LogWarning($"[(≧ ﹏ ≦){DateTime.UtcNow}]User Create Failed! {request.Email}");
            //    return false;
            //}

            //await _userRepository.AddOneByUserAsync(userTrc);
            //_logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]User Created! {request.Email}");
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            if(await _identityDomainSignUpServer.SignUpWhitEmailAsync(request.Email, request.Password,request.RoleName) is not UserAccessResult.Success)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){DateTime.UtcNow}]User Already Exists! {request.Email}");
                return false;
            }
            await _userRoleRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
            await _notMediator.SendAsync(new GenerateCodeCommand(request.Email, request.RoleName), cancellationToken);
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