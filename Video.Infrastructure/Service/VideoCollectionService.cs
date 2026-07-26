using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.Server;

namespace Video.Infrastructure.Service;

/// <summary>
/// Video collection service implementation.
/// Pure delegation to the repository — no caching layer needed
/// for collection metadata at this stage.
/// </summary>
public class VideoCollectionService : IVideoCollectionService
{
    private readonly IVideoCollectionRepository _videoCollectionRepository;

    public VideoCollectionService(IVideoCollectionRepository videoCollectionRepository)
    {
        _videoCollectionRepository = videoCollectionRepository;
    }

    public async Task<VideoCollection> GetByVideoCollectionAsync(Guid videoCollectionGuid)
    {
        return await _videoCollectionRepository.FindByVideoCollectionAsync(videoCollectionGuid);
    }

    public async Task<VideoCollection> GetByVideoCollectionAsync(string videoCollectionName)
    {
        return await _videoCollectionRepository.FindByVideoCollectionAsync(videoCollectionName);
    }

    public async Task<List<VideoCollection>> BlurredByVideoCollectionAsync(string blurredVideoCollection)
    {
        return await _videoCollectionRepository.BlurredByVideoCollectionAsync(blurredVideoCollection);
    }

    public async Task<List<VideoCollection>> PageByVideoCollectionAsync(int page, int pageSize)
    {
        return await _videoCollectionRepository.PageByVideoCollectionAsync(page, pageSize);
    }

    public async Task<List<VideoCollection>> GetByVideoCollectionListAsync()
    {
        return await _videoCollectionRepository.FindByVideoCollectionListAsync();
    }
}
