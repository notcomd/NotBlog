namespace FileDev.Web.API.Application.Commands;

/// <summary>删除文件（软删除，S-08 归属校验由领域服务执行）</summary>
public class DeleteFileCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid FileId { get; set; }
}
