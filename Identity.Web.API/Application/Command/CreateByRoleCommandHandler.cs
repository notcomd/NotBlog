using Identity.Domain.IdentiyResult;

namespace Identity.Web.API.Application.Command;

public class CreateByRoleCommandHandler : IRequestHandler<CreateByRoleCommand, bool>
{

    private readonly IUserRoleRepository _userRoleRepository;
    private readonly INotDateTime _notDateTime;
    private readonly ILogger<CreateByRoleCommandHandler> _logger;
    private readonly IdentityDomainRoleManagerServer _identityDomainRoleManagerServer;


    public CreateByRoleCommandHandler(IUserRoleRepository userRoleRepository, INotDateTime notDateTime, ILogger<CreateByRoleCommandHandler> logger, IdentityDomainRoleManagerServer identityDomainRoleManagerServer)
    {
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notDateTime = notDateTime ?? throw new ArgumentNullException(nameof(notDateTime));
        _identityDomainRoleManagerServer = identityDomainRoleManagerServer;
    }



    public async Task<bool> Handler(CreateByRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request is null) throw new ArgumentNullException(nameof(request));
            //var roleAuthority=request!.RoleAuthority;
            var registerRoleDto = new RegisterWithRoleDto(request.RoleName,
                request.Attribute, (EnRoleAuthority)request.RoleAuthority, (EnRoleStatus)request.RoleStatus);
            var signal = await _identityDomainRoleManagerServer.RegisterWithRoleAsync(registerRoleDto);

            if (signal == UserAccessResult.Success)
            {
<<<<<<< HEAD
                await _userRoleRepository.UnitOfWork.SavaChangesAsync(cancellationToken);
                _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow}] Role Create Success! {request.RoleName}");
=======
                _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]Role Created! RoleName: {request.RoleName}, Attribute: {request.Attribute}");
                var roleData = await _userRoleRepository.FindByUserRoleAsync(request.RoleName);
                if (roleData is not null)
                {
                    _logger.LogWarning($"[(≧ ﹏ ≦){DateTime.UtcNow}]Role Already Exists! {request.RoleName}");
                    return false;
                }
                var roleTrc = Roles.CreateByRoleAsync(request.RoleName, request.Attribute, _notDateTime.UtcNow);
                _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]Role Created! RoleName: {request.RoleName}, Attribute: {request.Attribute}");
                await _userRoleRepository.AddByUserRoleAsync(roleTrc);
                await _userRoleRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
                _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]Role Added to Repository! RoleName: {request.RoleName}, Attribute: {request.Attribute}");
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
                return true;
            }
            else if (signal == UserAccessResult.AlreadyExists)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] Role Already Exists! {request.RoleName}");
                return false;
            }
            else
            {
                _logger.LogError($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] Role Create Failed! {request.RoleName}");
                return false;
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[(≧ ﹏ ≦){DateTime.UtcNow}]Role Create Failed! {request.RoleName}");
            return false;
        }

    }

}
