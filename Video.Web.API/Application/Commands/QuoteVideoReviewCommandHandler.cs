


using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;


public class QuoteVideoReviewCommandHandler(IVideoCacheService videoCacheService,
IVideoRepository videoRepository,ILogger<QuoteVideoReviewCommandHandler> logger):IRequestHandler<QuoteVideoReviewCommand,bool>{

    private readonly ILogger<QuoteVideoReviewCommandHandler> _logger=logger??throw new ArgumentNullException(nameof(logger));
    private readonly IVideoCacheService _videoCacheService=videoCacheService??throw new ArgumentNullException(nameof(videoCacheService));
    private readonly IVideoRepository _videoRepository=videoRepository??throw new ArgumentNullException(nameof(videoRepository));

    public async Task<bool> Handler(QuoteVideoReviewCommand request, CancellationToken cancellationToken)
    {     
        return true;
    }

}