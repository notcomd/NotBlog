using Markdown.Web.API.Application.IntegrationEvents;
using Notcomd.Evenbus;

namespace Markdown.Web.API.Application.IntegrationEventHandlers;

/// <summary>
///     MarkReview 创建集成事件处理器
/// </summary>
[EvenBusName("MarkReviewCreated")]
public class MarkReviewCreatedEventHandler : JsonIntegrationEventHandler<MarkReviewCreatedIntegrationEvent>
{
    protected override Task EventDlerJson(string eventName, MarkReviewCreatedIntegrationEvent? eventData)
    {
        if (eventData == null)
            return Task.CompletedTask;

        // 处理跨服务的业务逻辑
        // 例如：发送推送通知、更新搜索引擎索引等
        Console.WriteLine($"[集成事件] 收到评论创建通知：{eventData.MarkReviewGuid}, 用户：{eventData.UserName}");

        return Task.CompletedTask;
    }
}

/// <summary>
///     MarkReview 删除集成事件处理器
/// </summary>
[EvenBusName("MarkReviewDeleted")]
public class MarkReviewDeletedEventHandler : JsonIntegrationEventHandler<MarkReviewDeletedIntegrationEvent>
{
    protected override Task EventDlerJson(string eventName, MarkReviewDeletedIntegrationEvent? eventData)
    {
        if (eventData == null)
            return Task.CompletedTask;

        // 清理其他服务的缓存等
        Console.WriteLine($"[集成事件] 收到评论删除通知：{eventData.MarkReviewGuid}");

        return Task.CompletedTask;
    }
}

/// <summary>
///     MarkReview 点赞集成事件处理器
/// </summary>
[EvenBusName("MarkReviewLiked")]
public class MarkReviewLikedEventHandler : JsonIntegrationEventHandler<MarkReviewLikedIntegrationEvent>
{
    protected override Task EventDlerJson(string eventName, MarkReviewLikedIntegrationEvent? eventData)
    {
        if (eventData == null)
            return Task.CompletedTask;

        // 实时更新计数、推送通知等
        Console.WriteLine($"[集成事件] 收到点赞通知：评论={eventData.MarkReviewGuid}, 新计数={eventData.NewLoveCount}");

        return Task.CompletedTask;
    }
}

/// <summary>
///     子评论添加集成事件处理器
/// </summary>
[EvenBusName("ChildReviewAdded")]
public class ChildReviewAddedEventHandler : JsonIntegrationEventHandler<ChildReviewAddedIntegrationEvent>
{
    protected override Task EventDlerJson(string eventName, ChildReviewAddedIntegrationEvent? eventData)
    {
        if (eventData == null)
            return Task.CompletedTask;

        // 处理子评论添加的跨服务逻辑
        Console.WriteLine($"[集成事件] 收到子评论添加通知：父={eventData.ParentReviewGuid}, 子={eventData.ChildReviewGuid}");

        return Task.CompletedTask;
    }
}