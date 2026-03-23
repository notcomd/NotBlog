using Markdown.Domain.Entities;
using NotMediator;

namespace Markdown.Domain.Events;

/// <summary>
///     MarkReview 创建领域事件
/// </summary>
public record MarkReviewCreatedDomainEvent(
    Guid MarkReviewGuid,
    Guid MarkDownGuid,
    Guid UserId,
    string Content,
    IEnumerable<ReviewImage> ReviewImage,
    DateTime CreatedAt
) : INotifications;

/// <summary>
///     MarkReview 删除领域事件
/// </summary>
public record MarkReviewDeletedDomainEvent(
    Guid MarkReviewGuid,
    Guid MarkDownGuid,
    DateTime DeletedAt
) : INotifications;

/// <summary>
///     MarkReview 权限更新领域事件
/// </summary>
public record MarkReviewAuthUpdatedDomainEvent(
    Guid MarkReviewGuid,
    MarkReviewAuth OldAuth,
    MarkReviewAuth NewAuth,
    DateTime UpdatedAt
) : INotifications;

/// <summary>
///     MarkReview 点赞领域事件
/// </summary>
public record MarkReviewLikedDomainEvent(
    Guid MarkReviewGuid,
    Guid LikedByUserId,
    long NewLoveCount,
    DateTime LikedAt
) : INotifications;

/// <summary>
///     子评论添加领域事件
/// </summary>
public record ChildReviewAddedDomainEvent(
    Guid ParentReviewGuid,
    Guid ChildReviewGuid,
    Guid MarkDownGuid,
    DateTime AddedAt
) : INotifications;