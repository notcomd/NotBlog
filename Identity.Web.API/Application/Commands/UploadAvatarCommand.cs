namespace Identity.Web.API.Application.Commands;


public record UploadAvatarCommand(
    Guid UserId,
    string FileName,
    byte[] ImageContent,
    string ContentType
) : IRequest<UploadAvatarResult>, ILoggableCommand
{
    public string IdProperty => nameof(UserId);
    public string IdValue => UserId.ToString();
}


public record UploadAvatarResult(
    string FileId,
    string FileUri,
    string FileMd5,
    long FileSize,
    int Width,
    int Height,
    string Format
);
