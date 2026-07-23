using Video.Domain.Entities;
using Video.Domain.IRepository;

namespace Video.Domain.Server;

public class VideoCollectionService
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

    public async Task AddByVideoCollectionAsync(VideoCollection addVideoCollection)
    {
        await _videoCollectionRepository.AddByVideoCollectionAsync(addVideoCollection);
    }

    public async Task UpdateByVideoCollectionAsync(VideoCollection updateVideoCollection)
    {
        await _videoCollectionRepository.UpdateByVideoCollectionAsync(updateVideoCollection);
    }
}