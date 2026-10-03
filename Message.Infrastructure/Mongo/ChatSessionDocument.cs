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

    /// <summary>会话类型。</summary>
    public SessionType SessionType { get; set; }
    /// <summary>关联群组ID（非群会话为空）。</summary>
    public Guid? GroupId { get; set; }
    /// <summary>关联圈子ID（非社区频道会话为空）。</summary>
    public Guid? CircleId { get; set; }
    /// <summary>创建者ID。</summary>
    public Guid CreatorId { get; set; }

    /// <summary>参与者ID列表</summary>
    public List<Guid> Participants { get; set; } = new();

    /// <summary>各成员的会话状态（未读 / 最后读取 / 置顶 / 免打扰），key = 用户ID</summary>
    public List<ChatSessionMemberStateDocument> MemberStates { get; set; } = new();

    /// <summary>最后一条消息ID。</summary>
    public Guid? LastMessageId { get; set; }
    /// <summary>最后一条消息内容摘要。</summary>
    public string? LastMessageContent { get; set; }
    /// <summary>最后一条消息时间。</summary>
    public DateTime? LastMessageTime { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreatedTime { get; set; }
    /// <summary>解散时间。</summary>
    public DateTime? DismissedTime { get; set; }
    /// <summary>是否已解散。</summary>
    public bool IsDismissed { get; set; }

    /// <summary>乐观锁版本号（__v，替换 EF ConcurrencyToken）</summary>
    public int Version { get; set; }
}