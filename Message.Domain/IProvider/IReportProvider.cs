using Message.Domain.Dto;
using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;

namespace Message.Domain.IProvider;

public interface IReportProvider
{
    Task<TweetReport> SubmitReportAsync(Guid reporterGuid, string targetType, Guid targetGuid,
        string reason, string category, IEnumerable<string>? evidenceUrls = null);

    Task<IEnumerable<TweetReport>> GetMyReportsAsync(Guid reporterGuid, int page = 1, int pageSize = 20);
}
