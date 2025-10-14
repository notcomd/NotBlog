

using NotMediator;
using FileDev.Domain.DomainEvent;
namespace FileDev.Web.API.Application.DomainEvnet;

public class CreateFileRepositoryDomainEventHandle : INotificationHandler<CreateFileRepositoryDomainEvent>
{
// 修正拼写错误，将 "Domian" 改为 "Domain"
    public  Task Handler(CreateFileRepositoryDomainEvent notification, CancellationToken cancellationToken){
        ArgumentNullException.ThrowIfNull(notification, nameof(notification));
        return Task.CompletedTask;
    }
}
