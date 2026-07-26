

using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 点赞视频评论命令
/// </summary>
public record QuoteVideoReviewCommand(Guid VideoGuid,Guid UserGuid,
Guid ReviewGuid,string Field):IRequest<bool>{
    public DateTime CreateAt=DateTime.UtcNow;
}