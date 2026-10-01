using FileDev.Domain.IRepository;

namespace FileDev.Web.API.DependencyInjection;

/// <summary>
/// 文件 API 端点使用的服务聚合（供 <c>[FromServices]</c> 一次性注入多个仓储/服务/中介者）。
/// </summary>
public record FileServicesDi(
    INotFileRepository NotFileRepository,
    ILogger<FileServicesDi> Logger,
    INotFileTagRepository NotFileTagRepository,
    INotFileStorageService NotFileStorageService,
    INotMediator NotMediator,
    INotFileService NotFileService);