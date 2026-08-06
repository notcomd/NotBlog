namespace Message.Web.API.Dto.Response;

/// <summary>
/// 推文详情结果。
/// </summary>
/// <param name="Tweet">推文实体（不存在时为 null）</param>
/// <param name="IsLiked">当前用户是否已点赞</param>
/// <param name="IsFavorited">当前用户是否已收藏</param>
/// <param name="IsCoined">当前用户是否已投币</param>
public record TweetDetailResult(Tweet? Tweet, bool IsLiked, bool IsFavorited, bool IsCoined);

