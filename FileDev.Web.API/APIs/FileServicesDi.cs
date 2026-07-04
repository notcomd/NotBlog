using FileDev.Domain.IRepository;

namespace FileDev.Web.API.APIs;

public record FileServicesDi(
    INotFileRepository NotFileRepository,
    ILogger<FileServicesDi> Logger,
    INotFileGroupRepository NotFileGroupRepository,
    INotFileStorageService NotFileStorageService,
    INotFileService NotFileService);