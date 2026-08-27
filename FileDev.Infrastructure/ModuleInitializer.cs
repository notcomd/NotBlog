using Commons.Core;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using FileDev.Infrastructure.Repository;
using FileDev.Infrastructure.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MohuTianchi.Lite;

namespace FileDev.Infrastructure;

public class ModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        // 底层对象存储（Mohu-TianChi Lite）：单例装配。RootPath 落在 <StoragePath>/lite 下，
        // 与既有 Flat 布局（<StoragePath>/*）物理分离；开启块级去重与文件日志。
        service.AddSingleton<IObjectStorage>(sp =>
        {
            var cfg = sp.GetRequiredService<IOptions<NotFileStorageOptions>>().Value;
            var root = Path.Combine(AppContext.BaseDirectory, cfg.StoragePath, "lite");
            var logFile = Path.Combine(root, "logs", "lite.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
            return LiteStorage.Create(o =>
            {
                o.RootPath = root;
                o.ChunkSize = cfg.ChunkFileSize;
                o.Storage.EnableDeduplication = true;
                o.LogToConsole = false;
                o.LogToFile = true;
                o.LogFilePath = logFile;
            });
        });

        service.AddScoped<INotFileGroupRepository, NotFileGroupRepository>();
        service.AddScoped<INotFileRepository, NotFileRepository>();
        service.AddScoped<INotFileVolumeRepository, NotFileVolumeRepository>();
        service.AddScoped<IContentAttachmentRefRepository, ContentAttachmentRefRepository>();
        service.AddScoped<INotFileStorageService, MohuObjectStorageService>();
        service.AddScoped<INotFileService, NotFileService>();
        service.AddScoped<INotFileGroupService, NotFileGroupService>();
        service.AddScoped<INotFileVolumeService, NotFileVolumeService>();
        service.AddScoped<IContentAttachmentService, ContentAttachmentService>();
        service.AddScoped<IFileChunkRepository, FileChunkRepository>();
        service.AddScoped<IFileChunkManager, FileChunkManager>();
        service.AddScoped<IRequestManagement, RequestManagement>();
        service.AddScoped<FileStorageService>();
        
    }
}