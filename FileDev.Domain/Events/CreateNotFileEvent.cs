using FileDev.Domain.Entities;
using NotMediator;

namespace FileDev.Domain.Events;

public record CreateNotFileEvent(
    NotFile NotFile,
    Guid UserGuid,
    string FileName,
    Uri FilePath,
    long FileSize,
    string FileMd5,
    FileIdentity FileIdentity,
    FileType FileType)
    : INotifications;