namespace Identity.Web.API.Application.Command;

public class CreateByPhoneUserCommandHandler : IRequestHandler<CreateByPhoneUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<CreateByPhoneUserCommandHandler> _logger;
    private readonly INotDateTime _notDateTime;

    public CreateByPhoneUserCommandHandler(IUserRepository userRepository, IUserRoleRepository userRoleRepository, ILogger<CreateByPhoneUserCommandHandler> logger, INotDateTime notDateTime)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notDateTime = notDateTime;
    }

    public async Task<bool> Handler(CreateByPhoneUserCommand request, CancellationToken cancellationToken)
    {
        // _logger.LogInformation($"[{DateTime.UtcNow}]Phone User Created! Phone: {request.PhoneNumber}, Password: {request.Password}");
        try
        {

            ArgumentNullException.ThrowIfNull(request);
            //var userData = await _userRepository.FindOneByUserAsync(request.PhoneNumber);
            //var roleData = await _userRoleRepository.FindByUserRoleAsync(request.RoleName);
            //if (userData is not null || roleData is null)
            //{
            //    _logger.LogWarning($"[(≧ ﹏ ≦){DateTime.UtcNow}]User Already Exists! {request.PhoneNumber}");
            //    return false;
            //}

            //var salt = await HashHelper.GenerateSaltValueTask();
            //var stamp = await HashHelper.GenerateSecurityStamp();

            //var userTrc = new User(roleData.Id, request.PhoneNumber, request.Password, _notDateTime.NowOffset);
            //userTrc.UserSafety.ChangeByBlackOrWhiteStatus(EnumBlackOrWhite.AuthorityWhite);
            //userTrc.UserSafety.ChangeByUserStatus(EnumUserStatus.UnActive);
            //if (userTrc is null)
            //{
            //    _logger.LogWarning($"[(≧ ﹏ ≦){DateTime.UtcNow}]User Create Failed! {request.PhoneNumber}");
            //    return false;
            //}
            await _userRepository.AddOneByUserAsync(userTrc);
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
