using FileDev.Domain.Entities;

namespace FileDev.Domain.Dto.Request;

public record RequestUpdateFileDto( string FileName, HashSet<string>? FileTags, string? FileDescription, FileIdentity FileIdentity, string FileMd5);