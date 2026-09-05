namespace FileDev.Web.API.Application.Commands;

/// <summary>创建自定义标签。默认标签（图片/文档/文件/视频）由系统在注册时预置，不通过本命令创建。</summary>
public record CreateTagCommand(
    Guid UserId,
    string TagName,
    string? TagDescription = null) : IRequest<bool>;