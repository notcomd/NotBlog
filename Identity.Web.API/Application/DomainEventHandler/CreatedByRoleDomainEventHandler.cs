
namespace Identity.Web.API.Application.DomainEventHandler
{
    public class CreatedByRoleDomainEventHandler : INotificationHandler<CreatedByRoleDomainEvent>
    {

        private readonly INotDateTime _notDateTime;
        private readonly ILogger<CreatedByRoleDomainEventHandler> _logger;

        public CreatedByRoleDomainEventHandler(INotDateTime notDateTime, ILogger<CreatedByRoleDomainEventHandler> logger)
        {
            _notDateTime = notDateTime;
            _logger = logger;
        }

        public Task Handler(CreatedByRoleDomainEvent notifications, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

    }
}
