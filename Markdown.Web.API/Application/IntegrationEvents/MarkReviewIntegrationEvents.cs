namespace Markdown.Web.API.Application.IntegrationEvents;

/// <summary>
///     MarkReview 创建集成事件（用于跨服务通信）
/// </summary>
public record MarkReviewCreatedIntegrationEvent(
    Guid MarkReviewGuid,
    Guid MarkDownGuid,
    Guid UserId,
    string UserName,
    string Content,
    DateTimeOffset CreatedAt
) : IntegrationEvent;

/// <summary>
///     MarkReview 删除集成事件
/// </summary>
public record MarkReviewDeletedIntegrationEvent(
    Guid MarkReviewGuid,
    Guid MarkDownGuid,
    DateTimeOffset DeletedAt
) : IntegrationEvent;

/// <summary>
///     MarkReview 点赞集成事件
/// </summary>
public record MarkReviewLikedIntegrationEvent(
    Guid MarkReviewGuid,
    Guid MarkDownGuid,
    Guid LikedByUserId,
    long NewLoveCount,
    DateTimeOffset LikedAt
) : IntegrationEvent;

/// <summary>
///     子评论添加集成事件
/// </summary>
public record ChildReviewAddedIntegrationEvent(
    Guid ParentReviewGuid,
    Guid ChildReviewGuid,
    Guid MarkDownGuid,
    Guid UserId,
    DateTimeOffset AddedAt
) : IntegrationEvent;
