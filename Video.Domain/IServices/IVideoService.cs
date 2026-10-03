
namespace Video.Domain.IServices;

/// <summary>
/// Video query service contract — read-only query operations for the Videos aggregate.
/// All write operations are handled by CQRS commands.
/// </summary>
public interface IVideoService
{
    /// <summary>按视频 Guid 查询视频（缓存优先）。</summary>
    Task<Videos> GetByVideoAsync(Guid videoGuid);

    /// <summary>按视频名称查询视频。</summary>
    Task<Videos> GetByVideoAsync(string videoName);

    /// <summary>分页查询视频列表。</summary>
    /// <param name="index">页码</param>
    /// <param name="size">每页数量</param>
    Task<List<Videos>> PagesByVideosAsync(int index, int size);

    /// <summary>按名称模糊查询视频列表。</summary>
    Task<List<Videos>> BlurredByVideoAsync(string videoName);
}
