namespace Identity.Web.API.Application.Command
{
    public class ChangeByPasswordCommandHandler : IRequestHandler<ChangeByPasswodCommand, bool>
    {

        private readonly INotDateTime _notDateTime;
        private readonly IdentityDomainUserManagerServer _identityDomainUserManagerServer;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ChangeByPasswordCommandHandler> _logger;

        public ChangeByPasswordCommandHandler(INotDateTime notDateTime, IdentityDomainUserManagerServer identityDomainUserManagerServer,
            IUserRepository userRepository, ILogger<ChangeByPasswordCommandHandler> logger)
        {
            _notDateTime = notDateTime;
            _identityDomainUserManagerServer = identityDomainUserManagerServer;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> Handler(ChangeByPasswodCommand request, CancellationToken cancellationToken)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var changeByPasswordDto = new ChangeByUserPasswordDto(request.OldPassword, request.NewPassword);
                if (await _identityDomainUserManagerServer.ChangeByUserPasswordAsync(request.UserEmail, changeByPasswordDto))
                {
                    _logger?.LogWarning($"[(≧ ﹏ ≦){_notDateTime?.UtcNow}]ChangeByPasswordCommand Failed! {request.UserEmail}");
                    return false;
                }
                _logger?.LogInformation($"[（*＾-＾*）{_notDateTime?.UtcNow}] ChangeByPasswordCommand Success! {request.UserEmail}");
                await _userRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[(≧ ﹏ ≦){_notDateTime?.UtcNow}]ChangeByPasswordCommand Failed! {request.UserEmail}");
                return false;
            }
        }


    }
}
