using Video.Domain.Entities;
using Video.Domain.ValueObjects;
using Video.Domain.SeedWork;
namespace Video.Domain.IRepository;

public interface IVideoRepository:IRepository<Videos>
{
    public Task<List<Videos>> FindByVideoListAsync();

    public Task<Videos> FindByVideoAsync(Guid findVideoGuid);

    public Task<Videos> FindByNvidAsync(string nvId);

    public Task<Videos> FindByVideoAsync(string blurredVideoName);

    public Task<List<Videos>> PageByVideoAsync(int page, int pageSize);

    public Task<Videos> FindByVideoName(string name);

    public Task<List<Videos>> BlurredByVideoName(string videoName);


    public Task AddByVideoAsync(Videos addVideo);

    public Task AddByVideoRangeAsync(List<Videos> addVideos);

    public Task UpdateByQuoteAsync(VideoQuote videoQuote);

    public Task UpdateByControlAsync(VideoControl videoCollection);

    public Task UpdateByTimeSpaceAsync(TimeSpace timeSpace);

    public Task UpdateByVideoAsync(Videos videos);

    public Task DeleteByVideoControlAsync(VideoControl videoControl);

    public Task DeleteByVideoControlRangeAsync(List<VideoControl> videoControl);

    public Task InDeleteByVideoAsync(Videos videoControl);

    public Task InDeleteByVideoRangeAsync(List<Videos> videosList);
}