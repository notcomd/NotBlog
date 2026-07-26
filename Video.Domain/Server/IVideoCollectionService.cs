using Video.Domain.Entities;

namespace Video.Domain.Server;

/// <summary>
/// Video collection query service contract — read-only query operations.
/// All write operations are handled by CQRS commands.
/// </summary>
public interface IVideoCollectionService
{
    Task<VideoCollection> GetByVideoCollectionAsync(Guid videoCollectionGuid);
    Task<VideoCollection> GetByVideoCollectionAsync(string videoCollectionName);
    Task<List<VideoCollection>> BlurredByVideoCollectionAsync(string blurredVideoCollection);
    Task<List<VideoCollection>> PageByVideoCollectionAsync(int page, int pageSize);
    Task<List<VideoCollection>> GetByVideoCollectionListAsync();
}
