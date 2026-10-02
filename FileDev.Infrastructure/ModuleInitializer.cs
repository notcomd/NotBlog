using Commons.Core;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.Repository;
using FileDev.Infrastructure.Service;
using FileDev.Infrastructure.Service.FileBox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.DependencyInjection;

namespace FileDev.Infrastructure;

public class ModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        // 底层对象存储（Mono.FileBox.Lite）：内容寻址 + 全局去重 + 对象生命周期状态机。
        // RootPath 默认落在 <StoragePath>/filebox 下，与既有 Flat 布局（<StoragePath>/*）物理分离。
        // AddMonoFileBoxLite(options) 把传入的 options 直接注册为 FileBoxOptions 单例（空 options 先占位），
        // 随后用工厂覆盖注册真正带磁盘池运行的 FileBoxOptions（运行时读取配置）。
        service.AddMonoFileBoxLite();

        // 覆盖 FileBoxOptions：从 NotFileStorage 配置推导磁盘池与分块；可再被 "FileBox" 配置节覆盖。
        service.AddSingleton(sp =>
        {
            var cfg = sp.GetRequiredService<IOptions<NotFileStorageOptions>>().Value;
            var root = Path.Combine(AppContext.BaseDirectory, cfg.StoragePath, "filebox");
            Directory.CreateDirectory(root);

            var options = new FileBoxOptions
            {
                Storage = new StorageOptions
                {
                    Deduplication = DeduplicationMode.Global,
                    Chunking = new ChunkingOptions
                    {
                        // 分块默认关闭；仅当配置了分片大小时开启对象内固定分块
                        Enabled = cfg.ChunkFileSize > 0,
                        ChunkSizeBytes = cfg.ChunkFileSize
                    }
                }
            };
            options.Storage.Pools.Add(new PoolOptions
            {
                PoolId = "user-repo",
                RootPath = Path.Combine(root, "user-repo"),
                Tier = Mono.FileBox.Lite.Abstractions.Index.StorageTier.Hot,
                Enabled = true
            });
            options.Storage.Pools.Add(new PoolOptions
            {
                PoolId = "content-attachment",
                RootPath = Path.Combine(root, "content-attachment"),
                Tier = Mono.FileBox.Lite.Abstractions.Index.StorageTier.Hot,
                Enabled = true
            });
            // default 池：分片暂存与未指定类别对象的回退落盘池
            options.Storage.Pools.Add(new PoolOptions
            {
                PoolId = "default",
                RootPath = root,
                Tier = Mono.FileBox.Lite.Abstractions.Index.StorageTier.Hot,
                Enabled = true
            });

            // 配置优先："FileBox" 配置节可覆盖默认推导（Pools/RootPath/Deduplication/Chunking 等）
            var configuration = sp.GetRequiredService<IConfiguration>();
            var section = configuration.GetSection("FileBox");
            if (section.Exists())
                section.Bind(options);
            return options;
        });

        // 持久化 JSON 索引：path→ContentHash 反查依赖它，重启后映射不丢失
        service.AddMonoFileBoxLiteIndex(b =>
            b.UseJsonFileEntryStore(Path.Combine(AppContext.BaseDirectory,
                Path.Combine("FileStorage", "filebox", "index", "entries.json"))));
        service.AddMonoFileBoxLiteStorage(b => b.UseDiskSelector<CategoryPoolSelector>());
        service.AddMonoFileBoxLiteUseCases();

        service.AddScoped<INotFileTagRepository, NotFileTagRepository>();
        service.AddScoped<INotFileRepository, NotFileRepository>();
        service.AddScoped<INotFileVolumeRepository, NotFileVolumeRepository>();
        service.AddScoped<IContentAttachmentRefRepository, ContentAttachmentRefRepository>();
        service.AddScoped<IUserFileInfoRepository, UserFileInfoRepository>();
        service.AddScoped<INotFileStorageService, FileBoxObjectStorageService>();
        service.AddScoped<ITenantContext, AsyncLocalTenantContext>();
        service.AddScoped<INotFileService, NotFileService>();
        service.AddScoped<INotFileVolumeService, NotFileVolumeService>();
        service.AddScoped<IContentAttachmentService, ContentAttachmentService>();
        service.AddScoped<IFileChunkRepository, MongoFileChunkRepository>();
        service.AddScoped<IFileChunkManager, FileChunkManager>();
        service.AddScoped<IRequestManagement, RequestManagement>();
        service.AddScoped<FileStorageService>();
    }
}