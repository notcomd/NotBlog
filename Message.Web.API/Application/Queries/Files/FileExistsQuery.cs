namespace Message.Web.API.Application.Queries.Files;

/// <summary>
/// 检查文件是否存在查询（无权访问视为不存在）。
/// </summary>
public record FileExistsQuery(Guid FileId) : IRequest<bool>;
