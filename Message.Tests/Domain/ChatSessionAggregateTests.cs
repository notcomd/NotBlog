using Message.Domain.Entities.Chat;
using Message.Domain.Enums;
using Message.Domain.Events;
using Commons.SeedWork;

namespace Message.Tests.Domain;

/// <summary>
/// 会话聚合根（<see cref="ChatSession"/>）单元测试。
/// 覆盖 DDD 聚合根行为：参与者管理、未读数、已读、解散与领域事件。
/// </summary>
[TestFixture]
public class ChatSessionAggregateTests
{
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private static readonly Guid UserC = Guid.NewGuid();

    [Test]
    public void CreatePrivateSession_应包含双方参与者并触发领域事件()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);

        Assert.That(session.SessionType, Is.EqualTo(SessionType.Private));
        Assert.That(session.IsParticipant(UserA), Is.True);
        Assert.That(session.IsParticipant(UserB), Is.True);
        Assert.That(session.IsParticipant(UserC), Is.False);
        Assert.That(session.DomainEvents,
            Has.One.TypeOf<SessionCreatedEvent>(), "创建会话必须产生 SessionCreatedEvent 领域事件");
    }

    [Test]
    public void CreateGroupSession_应合并创建者与初始成员()
    {
        var groupId = Guid.NewGuid();
        var members = new HashSet<Guid> { UserA, UserC };

        var session = ChatSession.CreateGroupSession(groupId, UserB, "测试群", members);

        Assert.That(session.SessionType, Is.EqualTo(SessionType.Group));
        Assert.That(session.GroupId, Is.EqualTo(groupId));
        Assert.That(session.SessionName, Is.EqualTo("测试群"));
        Assert.That(session.IsParticipant(UserA), Is.True);
        Assert.That(session.IsParticipant(UserB), Is.True, "创建者必须为参与者");
        Assert.That(session.IsParticipant(UserC), Is.True);
    }

    [Test]
    public void ChatSession_应实现聚合根标记接口()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);
        Assert.That(session, Is.InstanceOf<IAggregateRoot>(), "会话应作为聚合根被仓储整体持久化");
    }

    [Test]
    public void AddParticipant_重复添加应抛出异常()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);

        Assert.That(() => session.AddParticipant(UserB),
            Throws.InvalidOperationException.With.Message.Contains("已在会话中"));
    }

    [Test]
    public void RemoveParticipant_私聊仅剩两人时不允许移除()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);

        Assert.That(() => session.RemoveParticipant(UserA),
            Throws.InvalidOperationException.With.Message.Contains("至少需要两个参与者"));
    }

    [Test]
    public void RemoveParticipant_群聊可正常移除成员()
    {
        var groupId = Guid.NewGuid();
        var session = ChatSession.CreateGroupSession(
            groupId, UserA, "群", new HashSet<Guid> { UserB, UserC });

        session.RemoveParticipant(UserC);

        Assert.That(session.IsParticipant(UserC), Is.False);
        Assert.That(session.Participants, Has.Count.EqualTo(2));
    }

    [Test]
    public void UpdateLastMessage_应为除发送者外的参与者累计未读数()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);
        session.MarkAsRead(UserB);

        session.UpdateLastMessage(Guid.NewGuid(), "你好");

        Assert.That(session.LastMessageContent, Is.EqualTo("你好"));
        Assert.That(session.LastMessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(session.GetUnreadCount(UserB), Is.EqualTo(1));
    }

    [Test]
    public void MarkAsRead_应清零未读并记录读取时间()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);
        session.UpdateLastMessage(Guid.NewGuid(), "你好");

        session.MarkAsRead(UserB);

        Assert.That(session.GetUnreadCount(UserB), Is.Zero);
        Assert.That(session.LastReadTime[UserB], Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public void MarkAsRead_非参与者应抛出异常()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);

        Assert.That(() => session.MarkAsRead(UserC),
            Throws.InvalidOperationException.With.Message.Contains("不在会话中"));
    }

    [Test]
    public void Dismiss_后不得再进行任何变更操作()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);
        session.Dismiss();

        Assert.That(session.IsDismissed, Is.True);
        Assert.That(session.DismissedTime, Is.Not.Null);
        Assert.That(() => session.UpdateLastMessage(Guid.NewGuid(), "x"),
            Throws.InvalidOperationException.With.Message.Contains("已解散"));
        Assert.That(() => session.AddParticipant(UserC),
            Throws.InvalidOperationException.With.Message.Contains("已解散"));
    }

    [Test]
    public void Dismiss_重复解散应抛出异常()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);
        session.Dismiss();

        Assert.That(() => session.Dismiss(), Throws.InvalidOperationException);
    }

    [Test]
    public void UpdateSessionName_空名称应抛出异常()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);

        Assert.That(() => session.UpdateSessionName("  "),
            Throws.ArgumentException.With.Message.Contains("会话名称不能为空"));
    }

    [Test]
    public void Pin与Mute_应正确切换状态()
    {
        var session = ChatSession.CreatePrivateSession(UserA, UserB);

        session.Pin();
        Assert.That(session.IsPinned, Is.True);
        session.Unpin();
        Assert.That(session.IsPinned, Is.False);

        session.Mute();
        Assert.That(session.IsMuted, Is.True);
        session.Unmute();
        Assert.That(session.IsMuted, Is.False);
    }
}