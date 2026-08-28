using Message.Domain.Entities.Chat;

namespace Message.Infrastructure.Mongo;

/// <summary>
/// 聊天会话的 MongoDB 文档投影（D2-1：chat_session 集合）。
/// <para>
/// 纯 POCO，含 Mongo 与外部的物理耦合。仅作持久化中间态，
/// 读写时由 <see cref="MongoChatSessionRepository"/> 与领域聚合 <see cref="ChatSession"/> 互转，
/// 避免领域程序集引入 MongoDB 依赖。
/// </para>
/// </summary>
public sealed class ChatSessionDocument
{
    /// <summary>会话ID（类映射声明为 _id）</summary>
    public Guid SessionId { get; set; }

    public SessionType SessionType { get; set; }
    public string? SessionName { get; set; }
    public Guid? GroupId { get; set; }
    public Guid CreatorId { get; set; }

    /// <summary>参与者ID列表</summary>
    public List<Guid> Participants { get; set; } = new();

    /// <summary>各成员的会话状态（未读 / 最后读取 / 置顶 / 免打扰），key = 用户ID</summary>
    public List<ChatSessionMemberStateDocument> MemberStates { get; set; } = new();

    public Guid? LastMessageId { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageTime { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime? DismissedTime { get; set; }
    public bool IsDismissed { get; set; }

    /// <summary>乐观锁版本号（__v，替换 EF ConcurrencyToken）</summary>
    public int Version { get; set; }
}