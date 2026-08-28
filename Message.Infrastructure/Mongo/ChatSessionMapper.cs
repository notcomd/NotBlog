using Message.Domain.Entities.Chat;

namespace Message.Infrastructure.Mongo;

/// <summary>
/// <see cref="ChatSession"/>（领域聚合）与 <see cref="ChatSessionDocument"/>（Mongo 投影）互转。
/// 写路径由实体投影为文档；读路径由文档重建实体（经 <see cref="ChatSession.Rebuild"/>）。
/// </summary>
public static class ChatSessionMapper
{
    /// <summary>实体 → 文档</summary>
    public static ChatSessionDocument ToDocument(ChatSession session)
    {
        return new ChatSessionDocument
        {
            SessionId = session.SessionId,
            SessionType = session.SessionType,
            SessionName = session.SessionName,
            GroupId = session.GroupId,
            CreatorId = session.CreatorId,
            Participants = new List<Guid>(session.Participants),
            MemberStates = session.MemberStates.Values.Select(ToStateDocument).ToList(),
            LastMessageId = session.LastMessageId,
            LastMessageContent = session.LastMessageContent,
            LastMessageTime = session.LastMessageTime,
            CreatedTime = session.CreatedTime,
            DismissedTime = session.DismissedTime,
            IsDismissed = session.IsDismissed
        };
    }

    /// <summary>文档 → 实体</summary>
    public static ChatSession ToEntity(ChatSessionDocument doc)
    {
        var memberStates = doc.MemberStates.ToDictionary(
            d => d.MemberId, ToStateEntity);
        return ChatSession.Rebuild(
            doc.SessionId, doc.SessionType, doc.SessionName, doc.GroupId, doc.CreatorId,
            doc.Participants, memberStates,
            doc.LastMessageId, doc.LastMessageContent, doc.LastMessageTime,
            doc.CreatedTime, doc.DismissedTime, doc.IsDismissed);
    }

    /// <summary>领域值对象 → 文档子片段。</summary>
    private static ChatSessionMemberStateDocument ToStateDocument(ChatSessionMemberState state)
    {
        return new ChatSessionMemberStateDocument
        {
            MemberId = state.MemberId,
            UnreadCount = state.UnreadCount,
            LastReadTime = state.LastReadTime,
            IsPinned = state.IsPinned,
            IsMuted = state.IsMuted
        };
    }

    /// <summary>文档子片段 → 领域值对象。</summary>
    private static ChatSessionMemberState ToStateEntity(ChatSessionMemberStateDocument doc)
    {
        return ChatSessionMemberState.Rebuild(
            doc.MemberId, doc.UnreadCount, doc.LastReadTime, doc.IsPinned, doc.IsMuted);
    }
}