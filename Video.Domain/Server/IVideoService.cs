
namespace Video.Domain.Server;

/// <summary>
/// Video query service contract — read-only query operations for the Videos aggregate.
/// All write operations are handled by CQRS commands.
/// </summary>
public interface IVideoService
{
    Task<Videos> GetByVideoAsync(Guid videoGuid);
    Task<Videos> GetByVideoAsync(string videoName);
    Task<List<Videos>> PagesByVideosAsync(int index, int size);
    Task<List<Videos>> BlurredByVideoAsync(string videoName);
}
