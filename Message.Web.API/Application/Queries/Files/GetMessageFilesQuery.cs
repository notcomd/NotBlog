namespace Message.Web.API.Application.Queries.Files;

/// <summary>
/// 获取消息附件列表查询。
/// </summary>
public record GetMessageFilesQuery(Guid MessageId) : IRequest<IEnumerable<FileAttachment>>;
