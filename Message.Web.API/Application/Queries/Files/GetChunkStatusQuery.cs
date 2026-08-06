namespace Message.Web.API.Application.Queries.Files;

/// <summary>
/// 获取分片上传状态查询。
/// </summary>
public record GetChunkStatusQuery(string FileKey) : IRequest<ChunkStatusResult>;
