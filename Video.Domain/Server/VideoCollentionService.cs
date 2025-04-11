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
        => await _videoCollectionRepository.FindByVideoCollectionAsync(videoCollectionGuid);
}