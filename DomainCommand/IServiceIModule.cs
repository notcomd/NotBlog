using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public interface INotcomdServiceIModule
{
    public void NotcomdServiceModel(IServiceCollection service);
}