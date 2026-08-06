namespace Message.Web.API.Application.Queries.Files;

/// <summary>
/// 获取文件下载次数查询。
/// </summary>
public record GetDownloadCountQuery(Guid FileId) : IRequest<int>;
