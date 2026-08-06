
namespace Message.Web.API.Application.Queries.Files;
/// <summary>
/// 查询分片上传状态查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetChunkStatusQueryHandler(
    IFileStorageGrpcClient fileStorage) : IRequestHandler<GetChunkStatusQuery, ChunkStatusResult>
{
    public async Task<ChunkStatusResult> Handler(GetChunkStatusQuery query, CancellationToken cancellationToken)
        => await fileStorage.GetChunkStatusAsync(query.FileKey, cancellationToken);
}
