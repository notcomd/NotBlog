using Org.BouncyCastle.Bcpg;

namespace Identity.Web.API.Application.Command;

public class CreateByEmailUserCommandHandler : IRequestHandler<CreateByEmailUserCommand, bool>
{
   

    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<CreateByEmailUserCommandHandler> _logger;

    public CreateByEmailUserCommandHandler(IUserRepository userRepository, IUserRoleRepository userRoleRepository, ILogger<CreateByEmailUserCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

  

    public async Task<bool> Handler(CreateByEmailUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"[{DateTime.UtcNow}]Email Send! ");
        ArgumentNullException.ThrowIfNull(request);
        var userData=await _userRepository.FindOneByUserAsync(request.Email);
        var roleData = await _userRoleRepository.FindByUserRoleAsync(request.RoleName);
        if (userData is not null && roleData is not null)
        {
            _logger.LogWarning($"[{DateTime.UtcNow}]User Already Exists! {request.Email}");
            return false;
        }
        var userTrc= await User.CreateByEmailUser(roleData!.RoleGuid,request.Email, request.Password);
        if (userTrc is null)
        {
            _logger.LogWarning($"[{DateTime.UtcNow}]User Create Failed! {request.Email}");
            return false;
        }
        await _userRepository.AddOneByUserAsync(userTrc);
        _logger.LogInformation($"[{DateTime.UtcNow}]User Created! {request.Email}");
        await _userRoleRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        return true;
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