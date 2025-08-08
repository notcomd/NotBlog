namespace Identity.Web.API.Application.DomainEventHandler
{
    public class ChangeByUserStatusDomainEventHandler : INotificationHandler<ChangeByUserStatusDomainEvent>
    {

        private readonly ILogger<ChangeByUserStatusDomainEventHandler> _logger;
        private readonly INotDateTime _notDateTime;
        private readonly IEmail _email;

        public ChangeByUserStatusDomainEventHandler(ILogger<ChangeByUserStatusDomainEventHandler> logger, INotDateTime notDateTime, IEmail email)
        {
            _logger = logger;
            _notDateTime = notDateTime;
            _email = email;
        }

        public Task Handler(ChangeByUserStatusDomainEvent notifications, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
