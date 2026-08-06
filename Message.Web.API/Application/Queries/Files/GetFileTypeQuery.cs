namespace Message.Web.API.Application.Queries.Files;

/// <summary>
/// 获取文件类型查询。
/// </summary>
public record GetFileTypeQuery(Guid FileId) : IRequest<FileTypeQueryResult>;
