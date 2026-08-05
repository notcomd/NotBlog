namespace FileDev.Web.API.Application.Queries;

/// <summary>下载文件（S-08 归属校验 + 流式读取）</summary>
public class DownloadFileQuery : IRequest<FileDownloadResult>
{
    public Guid UserId { get; set; }
    public Guid FileId { get; set; }
}
