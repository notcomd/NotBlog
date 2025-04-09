using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public interface INotcomdServiceIModule
{
    public void Notcomd_Server_Initialize(IServiceCollection service);
}