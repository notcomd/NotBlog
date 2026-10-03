

namespace Markdown.Web.API.Application.IntegrationEvents.IntegrationEventHandlers;

/// <summary>
///MarkReview 创建集成事件处理器
/// </summary>
[EventBusName("MarkReviewCreated")]
public class MarkReviewCreatedEventHandler(ILogger<MarkReviewCreatedEventHandler> logger)
    : JsonIntegrationEventHandler<MarkReviewCreatedIntegrationEvent>
{
    public override Task Handler(MarkReviewCreatedIntegrationEvent eventData)
    {
        // 处理跨服务的业务逻辑
        // 例如：发送推送通知、更新搜索引擎索引等
        logger.LogInformation("[集成事件] 收到评论创建通知：{MarkReviewGuid}, 用户：{UserName}",
            eventData.MarkReviewGuid, eventData.UserName);

        return Task.CompletedTask;
    }
}

/// <summary>
///MarkReview 删除集成事件处理器
/// </summary>
[EventBusName("MarkReviewDeleted")]
public class MarkReviewDeletedEventHandler(ILogger<MarkReviewDeletedEventHandler> logger)
    : JsonIntegrationEventHandler<MarkReviewDeletedIntegrationEvent>
{
    public override Task Handler(MarkReviewDeletedIntegrationEvent eventData)
    {
        // 清理其他服务的缓存等
        logger.LogInformation("[集成事件] 收到评论删除通知：{MarkReviewGuid}", eventData.MarkReviewGuid);

        return Task.CompletedTask;
    }
}

/// <summary>
///MarkReview 添加子评论集成事件处理器
/// </summary>
[EventBusName("ChildReviewAdded")]
public class ChildReviewAddedEventHandler(ILogger<ChildReviewAddedEventHandler> logger)
    : JsonIntegrationEventHandler<ChildReviewAddedIntegrationEvent>
{
    public override Task Handler(ChildReviewAddedIntegrationEvent eventData)
    {
        // 处理子评论添加的跨服务逻辑
        logger.LogInformation("[集成事件] 收到子评论添加通知：父={ParentReviewGuid}, 子={ChildReviewGuid}",
            eventData.ParentReviewGuid, eventData.ChildReviewGuid);

        return Task.CompletedTask;
    }
}
