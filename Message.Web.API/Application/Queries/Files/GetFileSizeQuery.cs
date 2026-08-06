namespace Message.Web.API.Application.Queries.Files;

/// <summary>
/// 获取文件大小查询。
/// </summary>
public record GetFileSizeQuery(Guid FileId) : IRequest<string?>;
