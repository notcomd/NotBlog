namespace Identity.Web.API.Application.Command
{
    public class ChangeByUserSafetyCommandHandler : IRequestHandler<ChangeByUserSafetyCommand, bool>
    {

        private readonly INotDateTime _notDateTime;
        private readonly IdentityDomainUserManagerServer _identityDomainUserManagerServer;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ChangeByUserSafetyCommandHandler> _logger;

        public ChangeByUserSafetyCommandHandler(INotDateTime notDateTime, IdentityDomainUserManagerServer identityDomainUserManagerServer,
            IUserRepository userRepository, ILogger<ChangeByUserSafetyCommandHandler> logger)
        {
            _notDateTime = notDateTime;
            _identityDomainUserManagerServer = identityDomainUserManagerServer;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> Handler(ChangeByUserSafetyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request is null) throw new ArgumentNullException(nameof(ChangeByUserSafetyCommand));
                var changeBySafety = new ChangByUserSafetyDto(string.Empty, string.Empty, (EnBlackOrWhite)request.EnumBlackOrWhite, (EnUserStatus)request.EnumUserStatus, null);
                var signal = await _identityDomainUserManagerServer.ChangeWithUserSafetyAsync(request.UserEmail, changeBySafety);
                if (signal == Domain.IdentiyResult.UserAccessResult.Error)
                {
                    _logger?.LogError($"[(≧ ﹏ ≦){_notDateTime?.UtcNow}]ChangeByUserSafetyCommand Failed! {request.UserEmail}");
                    return false;
                }
                await _userRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
                _logger?.LogInformation($"[（*＾-＾*）{_notDateTime?.UtcNow}]ChangeByUserSafetyCommand Success! {request.UserEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime?.UtcNow}]ChangeByUserSafetyCommand Failed! {request.UserEmail}");
                return false;
            }
        }
    }
}
