using FileDev.Domain.IRepository;

namespace FileDev.Web.API.APIs;

public record FileServicesDi(
    INotFileRepository NotFileRepository,
    ILogger<FileServicesDi> Logger,
    INotFileTagRepository NotFileTagRepository,
    INotFileStorageService NotFileStorageService,
    INotMediator NotMediator,
    INotFileService NotFileService);