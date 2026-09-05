using Commons.Core;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.Repository;
using FileDev.Infrastructure.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MohuTianchi.Lite;

namespace FileDev.Infrastructure;

public class ModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        // 底层对象存储（Mohu-TianChi Lite）：单例装配。RootPath 默认落在 <StoragePath>/lite 下，
        // 与既有 Flat 布局（<StoragePath>/*）物理分离；开启块级去重与文件日志。
        // 配置优先使用 appsettings.json 的 LiteStorage 节（完整覆盖 LiteOptions），
        // 未显式配置的项回退到 NotFileStorage 选项推导的默认值。
        service.AddSingleton<IObjectStorage>(sp =>
        {
            var cfg = sp.GetRequiredService<IOptions<NotFileStorageOptions>>().Value;
            var configuration = sp.GetRequiredService<IConfiguration>();
            var liteSection = configuration.GetSection("LiteStorage");

            // 由现有 NotFile 配置推导的默认目录/日志位置
            var root = Path.Combine(AppContext.BaseDirectory, cfg.StoragePath, "lite");
            var logFile = Path.Combine(root, "logs", "lite.log");

            var options = new LiteOptions
            {
                RootPath = root,
                ChunkSize = cfg.ChunkFileSize,
                PersistIndex = true,
                LogToConsole = false,
                LogToFile = true,
                LogFilePath = logFile,
                Storage = new StorageOptions { EnableDeduplication = true },
                Lifecycle = new LifecycleOptions(),
                DirectoryLimits = new DirectoryLimits()
            };

            // 配置优先：LiteStorage 节覆盖默认值（嵌套 Storage/Lifecycle/DirectoryLimits 也一并填充）
            liteSection.Bind(options);

            // 相对路径统一解析到应用基目录，避免受当前工作目录影响
            options.RootPath = ResolveAbsolute(options.RootPath);
            options.LogFilePath = ResolveAbsolute(options.LogFilePath);
            if (!string.IsNullOrWhiteSpace(options.Storage.AutoGrowBasePath))
                options.Storage.AutoGrowBasePath = ResolveAbsolute(options.Storage.AutoGrowBasePath);

            Directory.CreateDirectory(Path.GetDirectoryName(options.LogFilePath)!);
            return LiteStorage.Create(o =>
            {
                o.RootPath = options.RootPath;
                o.ChunkSize = options.ChunkSize;
                o.DirectoryLimits = options.DirectoryLimits;
                o.PersistIndex = options.PersistIndex;
                o.LogLevel = options.LogLevel;
                o.LogToConsole = options.LogToConsole;
                o.LogToFile = options.LogToFile;
                o.LogFilePath = options.LogFilePath;
                o.Storage = options.Storage;
                o.Lifecycle = options.Lifecycle;
            });
        });

        service.AddScoped<INotFileTagRepository, NotFileTagRepository>();
        service.AddScoped<INotFileRepository, NotFileRepository>();
        service.AddScoped<INotFileVolumeRepository, NotFileVolumeRepository>();
        service.AddScoped<IContentAttachmentRefRepository, ContentAttachmentRefRepository>();
        service.AddScoped<IUserFileInfoRepository, UserFileInfoRepository>();
        service.AddScoped<INotFileStorageService, MohuObjectStorageService>();
        service.AddScoped<INotFileService, NotFileService>();
        service.AddScoped<INotFileVolumeService, NotFileVolumeService>();
        service.AddScoped<IContentAttachmentService, ContentAttachmentService>();
        service.AddScoped<IFileChunkRepository, MongoFileChunkRepository>();
        service.AddScoped<IFileChunkManager, FileChunkManager>();
        service.AddScoped<IRequestManagement, RequestManagement>();
        service.AddScoped<FileStorageService>();
    }

    /// <summary>将相对路径解析为绝对路径（相对路径结合应用基目录）；绝对路径或空串原样返回。</summary>
    /// <param name="path">待解析路径。</param>
    private static string ResolveAbsolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
            return path;
        return Path.Combine(AppContext.BaseDirectory, path);
    }
}