using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.Events;
using Commons.SeedWork;
using DomainMessage = Message.Domain.Entities.Message;

namespace Message.Tests.Domain;

/// <summary>
/// 消息聚合根（<see cref="Message"/>）单元测试。
/// 覆盖消息生命周期状态机（待发送→已发送→已送达→已读）与撤回规则。
/// </summary>
[TestFixture]
public class MessageAggregateTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid SenderId = Guid.NewGuid();

    [Test]
    public void CreateTextMessage_应初始化待发送状态并触发领域事件()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");

        Assert.That(message.MessageType, Is.EqualTo(MessageType.MessageText));
        Assert.That(message.Status, Is.EqualTo(MessageStatus.Pending));
        Assert.That(message.Content, Is.EqualTo("你好"));
        Assert.That(message.IsRecalled, Is.False);
        Assert.That(message.DomainEvents,
            Has.One.TypeOf<MessageSentEvent>(), "创建消息必须产生 MessageSentEvent 领域事件");
    }

    [Test]
    public void CreateImageMessage_应保存媒体地址与说明()
    {
        var uri = new Uri("https://example.com/a.png");
        var message = DomainMessage.CreateImageMessage(SessionId, SenderId, uri, "配图", "https://example.com/t.png");

        Assert.That(message.MessageType, Is.EqualTo(MessageType.MessageImage));
        Assert.That(message.MediaUri, Is.EqualTo(uri));
        Assert.That(message.Caption, Is.EqualTo("配图"));
        Assert.That(message.ThumbnailUri, Is.EqualTo("https://example.com/t.png"));
    }

    [Test]
    public void CreateExpressionMessage_空表情代码应抛出异常()
    {
        Assert.That(() => DomainMessage.CreateExpressionMessage(SessionId, SenderId, " "),
            Throws.ArgumentException.With.Message.Contains("表情代码不能为空"));
    }

    [Test]
    public void CreateLinkMessage_应保存链接信息()
    {
        var message = DomainMessage.CreateLinkMessage(SessionId, SenderId, "https://example.com/a", "标题", "描述");

        Assert.That(message.MessageType, Is.EqualTo(MessageType.MessageLink));
        Assert.That(message.LinkUrl, Is.EqualTo("https://example.com/a"));
        Assert.That(message.LinkTitle, Is.EqualTo("标题"));
        Assert.That(message.LinkDescription, Is.EqualTo("描述"));
    }

    [Test]
    public void 消息状态机_应按Pending_Sent_Delivered_Read顺序流转()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");
        message.SetReceiver(Guid.NewGuid());

        message.MarkAsSent();
        Assert.That(message.Status, Is.EqualTo(MessageStatus.Sent));

        message.MarkAsDelivered();
        Assert.That(message.Status, Is.EqualTo(MessageStatus.Delivered));
        Assert.That(message.DeliveredTime, Is.Not.Null);
        Assert.That(message.DomainEvents, Has.One.TypeOf<MessageReceivedEvent>());

        message.MarkAsRead();
        Assert.That(message.Status, Is.EqualTo(MessageStatus.Read));
        Assert.That(message.ReadTime, Is.Not.Null);
        Assert.That(message.DomainEvents, Has.One.TypeOf<MessageReadEvent>());
    }

    [Test]
    public void MarkAsSent_非Pending状态应抛出异常()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");
        message.MarkAsSent();

        Assert.That(() => message.MarkAsSent(), Throws.InvalidOperationException);
    }

    [Test]
    public void Recall_时限内撤回应标记为已撤回()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");

        message.Recall(SenderId, RecallReason.UserRequest, "你好");

        Assert.That(message.IsRecalled, Is.True);
        Assert.That(message.Status, Is.EqualTo(MessageStatus.Recalled));
        Assert.That(message.DomainEvents, Has.One.TypeOf<MessageRecalledEvent>());
    }

    [Test]
    public void Recall_超过时限应抛出异常()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");
        // 模拟消息发送于 10 分钟前，超过默认 2 分钟撤回时限
        typeof(DomainMessage)
            .GetProperty(nameof(DomainMessage.SentTime))!
            .SetValue(message, DateTime.UtcNow.AddMinutes(-10));

        Assert.That(() => message.Recall(SenderId, RecallReason.UserRequest, "你好"),
            Throws.InvalidOperationException.With.Message.Contains("超过撤回时限"));
    }

    [Test]
    public void Recall_重复撤回应抛出异常()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");
        message.Recall(SenderId, RecallReason.UserRequest, "你好");

        Assert.That(() => message.Recall(SenderId, RecallReason.UserRequest, "你好"),
            Throws.InvalidOperationException.With.Message.Contains("已撤回"));
    }

    [Test]
    public void SetReceiver_重复设置应抛出异常()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");
        message.SetReceiver(Guid.NewGuid());

        Assert.That(() => message.SetReceiver(Guid.NewGuid()),
            Throws.InvalidOperationException.With.Message.Contains("接收者已设置"));
    }

    [Test]
    public void MarkAsForwarded_应记录原始消息ID()
    {
        var originalId = Guid.NewGuid();
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");

        message.MarkAsForwarded(originalId);

        Assert.That(message.IsForwarded, Is.True);
        Assert.That(message.OriginalMessageId, Is.EqualTo(originalId));
        Assert.That(message.DomainEvents, Has.One.TypeOf<MessageForwardedEvent>());
    }

    [Test]
    public void Message_应实现聚合根标记接口()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, SenderId, "你好");
        Assert.That(message, Is.InstanceOf<IAggregateRoot>(), "消息应作为聚合根被仓储整体持久化");
    }
}
