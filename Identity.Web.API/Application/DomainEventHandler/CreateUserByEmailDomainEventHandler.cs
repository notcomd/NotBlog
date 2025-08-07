
using Identity.Domain.INotDateTime;

namespace Identity.Web.API.Application.DomainEventHandler
{
    public class CreateUserByEmailDomainEventHandler :INotificationHandler<CreateUserByEmailDomainEvent>
    {

        private readonly INotDateTime _notDateTime;
        private readonly ILogger<CreateUserByEmailDomainEventHandler> _logger;

        public CreateUserByEmailDomainEventHandler(INotDateTime notDateTime, ILogger<CreateUserByEmailDomainEventHandler> logger)
        {
            _notDateTime = notDateTime;
            _logger = logger;
        }



        public Task Handler(CreateUserByEmailDomainEvent notifications, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
