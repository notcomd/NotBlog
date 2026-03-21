using DomainCommons;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using FileDev.Infrastructure.Repository;
using FileDev.Infrastructure.Service;
using Microsoft.Extensions.DependencyInjection;

namespace FileDev.Infrastructure;

public class ModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<INotFileGroupRepository, NotFileGroupRepository>();
        service.AddScoped<INotFileRepository, NotFileRepository>();
        service.AddScoped<INotFileStorageService, NotFileStorageService>();
        service.AddScoped<INotFileService, NotFileService>();
        service.AddScoped<INotFileGroupService, NotFileGroupService>();
        service.AddScoped<FileStorageService>();
    }
}