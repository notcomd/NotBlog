namespace Message.Web.API.Application.Queries.Files;

/// <summary>
/// 获取文件信息查询（无权访问返回 null）。
/// </summary>
public record GetFileQuery(Guid FileId) : IRequest<FileAttachment?>;
