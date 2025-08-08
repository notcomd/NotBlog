
namespace Identity.Web.API.Application.DomainEventHandler
{
    public class AccountLockedDomainEventHandler:INotificationHandler<AccountLockedDomainEvent>
    {

        private readonly INotDateTime _notDateTime;
        private readonly ILogger<AccountLockedDomainEventHandler> _logger;

        public AccountLockedDomainEventHandler(INotDateTime notDateTime, ILogger<AccountLockedDomainEventHandler> logger)
        {
            _notDateTime = notDateTime;
            _logger = logger;
        }


        public Task Handler(AccountLockedDomainEvent notifications, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

    }
}
