using Video.Domain.SeedWork;
using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

public class VideoReview : Entity
{
    public Guid VideoReviewGuid { get; init; }

    public Guid VideoGuid { get; init; }

    public Guid UserGuid { get; init; }

    public Guid? RootReview { get; private set; }

    public string? VideoReviewBody { get; init; }

    public TimeSpace TimeSpace { get; private set; }

    public VideoControl VideoControl { get; private set; }

    public VideoQuote VideoQuote { get; private set; }

    public ICollection<VideoReview>? VideoReviews { get; private set; }

    public ICollection<VideoImage> VideoImages { get; private set; }

    private VideoReview()
    {
        VideoReviewGuid = Guid.CreateVersion7();
        TimeSpace = new TimeSpace(DateTime.UtcNow, DateTime.UtcNow);
        VideoControl = VideoControl.VideoControlBuilder();
        VideoQuote = VideoQuote.VideoQuoteBuilder();
        VideoImages = [];
        VideoReviews = [];
    }
    
    /// <summary>
    /// 视频评论
    /// </summary>
    public VideoReview(Guid videoGuid, Guid userGuid, Guid? rootGuid, 
    string? videoReviewBody,
        List<VideoImage>? videoImages) : this()
    {
        VideoGuid = videoGuid;
        UserGuid = userGuid;
        RootReview = rootGuid;
        if (videoImages != null) VideoImages = videoImages;
        VideoReviewBody = videoReviewBody;
    }


    public void AddByRootReview(Guid rootGuid)
    {
        RootReview = rootGuid;
    }
    
    
    public class VideoReviewBuilder
    { 
        private Guid _videoGuid;
        private Guid _userGuid;
        private Guid? _rootGuid;
        private string? _videoReviewBody;
        private List<VideoImage>? _videoImages;
        public VideoReviewBuilder WithVideoGuid(Guid videoGuid)
        {
            _videoGuid = videoGuid;
            return this;
        }
        public VideoReviewBuilder WithUserGuid(Guid userGuid)
        {
            _userGuid = userGuid;
            return this;
        }
        public VideoReviewBuilder WithRootGuid(Guid? rootGuid)
        {
            _rootGuid = rootGuid;
            return this;
        }
        public VideoReviewBuilder WithVideoReviewBody(string? videoReviewBody)
        {
            _videoReviewBody = videoReviewBody;
            return this;
        }
        public VideoReviewBuilder WithVideoImages(List<VideoImage>? videoImages)
        {
            _videoImages = videoImages;
            return this;
        }
        public VideoReview Build()
        {
            return new VideoReview(_videoGuid, _userGuid, _rootGuid, _videoReviewBody, _videoImages);
        }
    }
    
    
}