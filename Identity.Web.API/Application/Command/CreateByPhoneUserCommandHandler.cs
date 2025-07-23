
namespace Identity.Web.API.Application.Command
{
    public class CreateByPhoneUserCommandHandler : IRequestHandler<CreateByPhoneUserCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ILogger<CreateByPhoneUserCommandHandler> _logger;

        public CreateByPhoneUserCommandHandler(IUserRepository userRepository, IUserRoleRepository userRoleRepository, ILogger<CreateByPhoneUserCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handler(CreateByPhoneUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"[{DateTime.UtcNow}]Phone User Created! Phone: {request.PhoneNumber}, Password: {request.Password}");
            ArgumentNullException.ThrowIfNull(request);
            var userData = await _userRepository.FindOneByUserAsync(request.PhoneNumber);
            var roleData = await _userRoleRepository.FindByUserRoleAsync(request.RoleName);
            if (userData is not null)
            {
                _logger.LogWarning($"[{DateTime.UtcNow}]User Already Exists! {request.PhoneNumber}");
                return false;
            }

            if (roleData is null)
            {
                _logger.LogWarning($"[{DateTime.UtcNow}]Role Not Found! {request.RoleName}");
                return false;
            }

            var userTrc = await User.CreateByPhoneUser(roleData.RoleGuid, request.PhoneNumber, request.Password);
            if (userTrc is null)
            {
                _logger.LogWarning($"[{DateTime.UtcNow}]User Create Failed! {request.PhoneNumber}");
                return false;
            }
            await _userRepository.AddOneByUserAsync(userTrc);
            _logger.LogInformation($"[{DateTime.UtcNow}]User Created! {request.PhoneNumber}");
            await _userRoleRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
            _logger.LogInformation($"[{DateTime.UtcNow}]User Role Assigned! {request.PhoneNumber}, Role: {request.RoleName}");
            // Optionally, you can log the role assignment or any other relevant information here.
            return true;
        }
    }
}
