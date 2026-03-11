using System.Reflection;
using DomainCommonst;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public static class ModuleInitializer
{
    public static IServiceCollection AddAutoAddInstance(this IServiceCollection service,
        IEnumerable<Assembly> assemblies)
    {
        foreach (var itemAss in assemblies)
        {
            var typeAss = itemAss.GetTypes();
            var notcomdIModel = typeAss
                .Where(en => !en.IsAbstract && typeof(IModuleInitializer).IsAssignableFrom(en));
            foreach (var itemModel in notcomdIModel)
            {
                var initializer = (IModuleInitializer?)Activator.CreateInstance(itemModel);
                if (initializer == null) throw new ArgumentNullException($"{itemModel.Name} is null!");
                initializer.Initialize(service);
            }
        }

        return service;
    }
}