using Markdown.Web.API.Application.IntegrationEvents;
using Notcomd.EventBus.Core;
using Notcomd.EventBus.Extension; 

namespace Markdown.Web.API.Application.IntegrationEventHandlers;

/// <summary>
///     MarkReview 创建集成事件处理器
/// </summary>
[EventBusName("MarkReviewCreated")]
public class MarkReviewCreatedEventHandler : JsonIntegrationEventHandler<MarkReviewCreatedIntegrationEvent>
{
    public override Task Handler(MarkReviewCreatedIntegrationEvent eventData)
    {
        // 处理跨服务的业务逻辑
        // 例如：发送推送通知、更新搜索引擎索引等
        Console.WriteLine($"[集成事件] 收到评论创建通知：{eventData.MarkReviewGuid}, 用户：{eventData.UserName}");

        return Task.CompletedTask;
    }
}

/// <summary>
///     MarkReview 删除集成事件处理器
/// </summary>
[EventBusName("MarkReviewDeleted")]
public class MarkReviewDeletedEventHandler : JsonIntegrationEventHandler<MarkReviewDeletedIntegrationEvent>
{
    public override Task Handler(MarkReviewDeletedIntegrationEvent eventData)
    {
        // 清理其他服务的缓存等
        Console.WriteLine($"[集成事件] 收到评论删除通知：{eventData.MarkReviewGuid}");

        return Task.CompletedTask;
    }
}

/// <summary>
///     MarkReview 点赞集成事件处理器
/// </summary>
[EventBusName("MarkReviewLiked")]
public class MarkReviewLikedEventHandler : JsonIntegrationEventHandler<MarkReviewLikedIntegrationEvent>
{
    public override Task Handler(MarkReviewLikedIntegrationEvent eventData)
    {
        // 实时更新计数、推送通知等
        Console.WriteLine($"[集成事件] 收到点赞通知：评论={eventData.MarkReviewGuid}, 新计数={eventData.NewLoveCount}");

        return Task.CompletedTask;
    }
}

/// <summary>
///     子评论添加集成事件处理器
/// </summary>
[EventBusName("ChildReviewAdded")]
public class ChildReviewAddedEventHandler : JsonIntegrationEventHandler<ChildReviewAddedIntegrationEvent>
{
    public override Task Handler(ChildReviewAddedIntegrationEvent eventData)
    {
        // 处理子评论添加的跨服务逻辑
        Console.WriteLine($"[集成事件] 收到子评论添加通知：父={eventData.ParentReviewGuid}, 子={eventData.ChildReviewGuid}");

        return Task.CompletedTask;
    }
}
