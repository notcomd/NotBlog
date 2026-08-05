namespace FileDev.Web.API.Application.Queries;

/// <summary>获取单个文件信息（含越权校验，S-08）</summary>
public class GetFileInfoQuery : IRequest<NotFile>
{
    public Guid UserId { get; set; }
    public Guid FileId { get; set; }
}
