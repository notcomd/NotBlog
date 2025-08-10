
namespace Identity.Web.API.Application.DomainEventHandler;

public class AccountLockedDomainEventHandler:INotificationHandler<AccountLockedDomainEvent>
{

    private readonly INotDateTime _notDateTime;
    private readonly ILogger<AccountLockedDomainEventHandler> _logger;
    private readonly IEmail _email;
    private readonly INotDateTime _notDateTime1;

    public AccountLockedDomainEventHandler(INotDateTime notDateTime, ILogger<AccountLockedDomainEventHandler> logger, IEmail email, INotDateTime notDateTime1)
    {
        _notDateTime = notDateTime;
        _logger = logger;
        _email = email;
        _notDateTime1 = notDateTime1;
    }


    public Task Handler(AccountLockedDomainEvent notifications, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

}
