using Microsoft.Extensions.DependencyInjection;




namespace Notcomd.DomainCommand
{
    public interface INotcomd_ServiceIModule
    {
        public void Notcomd_Server_Initialize(IServiceCollection service);
    }
}
