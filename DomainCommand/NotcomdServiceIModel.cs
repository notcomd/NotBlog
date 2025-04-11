using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public static class NotcomdServiceIModel
{
    public static IServiceCollection AutoCreateInstance(this IServiceCollection service, IEnumerable<Assembly> assemblies)
    {
        foreach (var itemAss in assemblies)
        {
            var typeAss = itemAss.GetTypes();
            var notcomdIModel = typeAss
                .Where(en => !en.IsAbstract && typeof(INotcomdServiceIModule).IsInstanceOfType(en));
            foreach (var itemModel in notcomdIModel)
            {
                var initializer = (INotcomdServiceIModule?)Activator.CreateInstance(itemModel);
                if (initializer == null)
                {
                    throw new ArgumentNullException($"{itemModel.Name} is null!");
                }
                initializer.NotcomdServiceModel(service);
            }
        }
        return service;
    }
}