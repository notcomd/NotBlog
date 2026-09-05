namespace FileDev.Web.API.Dto.Response;
 
 public sealed record TagFileResponse(
        Guid FileId,
        string FileName,
        long FileSize,
        Uri FileUri,
        string? FileMd5,
        IReadOnlyList<string> FileTags,
        DateTimeOffset UploadTime);