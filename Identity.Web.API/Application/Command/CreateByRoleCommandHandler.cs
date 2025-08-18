namespace Identity.Web.API.Application.Command
{
    public class CreateByRoleCommandHandler : IRequestHandler<CreateByRoleCommand, bool>
    {

        private readonly IUserRoleRepository _userRoleRepository;
        private readonly INotDateTime _notDateTime;
        private readonly ILogger<CreateByRoleCommandHandler> _logger;


        public CreateByRoleCommandHandler(IUserRoleRepository userRoleRepository, INotDateTime notDateTime, ILogger<CreateByRoleCommandHandler> logger)
        {
            _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notDateTime = notDateTime ?? throw new ArgumentNullException(nameof(notDateTime));
        }



        public async Task<bool> Handler(CreateByRoleCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]Role Created! RoleName: {request.RoleName}, Attribute: {request.Attribute}");
                var roleData = await _userRoleRepository.FindByUserRoleAsync(request.RoleName);
                if (roleData is not null)
                {
                    _logger.LogWarning($"[(≧ ﹏ ≦){DateTime.UtcNow}]Role Already Exists! {request.RoleName}");
                    return false;
                }
                var roleTrc = await Roles.CreateByRoleAsync(request.RoleName, _notDateTime.NowOffset, request.Attribute);
                _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]Role Created! RoleName: {request.RoleName}, Attribute: {request.Attribute}");
                await _userRoleRepository.AddByUserRoleAsync(roleTrc);
                await _userRoleRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
                _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]Role Added to Repository! RoleName: {request.RoleName}, Attribute: {request.Attribute}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[(≧ ﹏ ≦){DateTime.UtcNow}]Role Create Failed! {request.RoleName}");
                return false;
            }

        }
    }
}
