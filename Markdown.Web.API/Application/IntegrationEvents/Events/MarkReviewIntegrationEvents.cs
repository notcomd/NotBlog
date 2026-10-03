namespace Markdown.Web.API.Application.IntegrationEvents.Events;

/// <summary>
///     MarkReview 创建集成事件（用于跨服务通信）
/// </summary>
[EventBusName("MarkReviewCreated")]
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
[EventBusName("MarkReviewDeleted")]
public record MarkReviewDeletedIntegrationEvent(
    Guid MarkReviewGuid,
    Guid MarkDownGuid,
    DateTimeOffset DeletedAt
) : IntegrationEvent;

/// <summary>
///     子评论添加集成事件
/// </summary>
[EventBusName("ChildReviewAdded")]
public record ChildReviewAddedIntegrationEvent(
    Guid ParentReviewGuid,
    Guid ChildReviewGuid,
    Guid MarkDownGuid,
    Guid UserId,
    DateTimeOffset AddedAt
) : IntegrationEvent;
