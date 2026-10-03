using Message.Infrastructure.EntityFramework;
using Message.Infrastructure.Mongo;
using MongoDB.Driver;
using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Infrastructure.MongoMigration;

/// <summary>
/// 存量聊天数据迁移（PG → Mongo）：读取 PostgreSQL 的 Messages / ChatSessions 全量数据，
/// 投影为 message + chat_session 集合文档后幂等写入 Mongo（ReplaceOne + upsert）。
/// <para>
/// 要点：
/// <list type="bullet">
/// <item>PG 的 <see cref="ChatSessionDocument.UnreadCount"/> 未持久化（EF Ignore）。迁移时按
/// <c>Messages.Status==Sent &amp;&amp; ReceiverId!=null</c> 分组聚合补算，保证会话未读徽标与 Mongo
/// message 文档（<c>(receiverId, status)</c> 补推索引）口径一致。</item>
/// <item>消息带附件内嵌：加载时 <c>Include(Messages.Attachments)</c>。</item>
/// <item>幂等：以文档主键（MessageId/SessionId）ReplaceOne+upsert，可安全重跑。</item>
/// </list>
/// </para>
/// </summary>
public class MessageMongoMigrationService(MessageDbContext context, IMongoDatabase database)
{
    /// <summary>消息批大小。</summary>
    private const int BatchSize = 500;

    private readonly IMongoCollection<ChatMessageDocument> _messages = MongoChatCollection.GetMessages(database);
    private readonly IMongoCollection<ChatSessionDocument> _sessions = MongoChatCollection.GetSessions(database);

    /// <summary>
    /// 执行存量迁移，返回迁移统计。
    /// </summary>
    public async Task<MigrationResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult
        {
            SessionsMigrated = await MigrateSessionsAsync(cancellationToken),
            MessagesMigrated = await MigrateMessagesAsync(cancellationToken)
        };
        return result;
    }

    /// <summary>迁移会话文档，并补算未读数。</summary>
    private async Task<int> MigrateSessionsAsync(CancellationToken cancellationToken)
    {
        // 补算未读：Messages.Status==Sent 且 ReceiverId 非空 → (SessionId, ReceiverId, Count)
        var unreadQuery = await context.Messages.AsNoTracking()
            .Where(m => m.Status == MessageStatus.Sent && m.ReceiverId != null)
            .GroupBy(m => new { m.SessionId, m.ReceiverId })
            .Select(g => new { g.Key.SessionId, ReceiverId = g.Key.ReceiverId!.Value, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var unreadBySession = new Dictionary<Guid, Dictionary<Guid, int>>();
        foreach (var item in unreadQuery)
        {
            if (!unreadBySession.TryGetValue(item.SessionId, out var inner))
            {
                inner = new Dictionary<Guid, int>();
                unreadBySession[item.SessionId] = inner;
            }
            inner[item.ReceiverId] = item.Count;
        }

        var sessions = await context.ChatSessions.AsNoTracking().ToListAsync(cancellationToken);
        var migrated = 0;

        foreach (var session in sessions)
        {
            var doc = ChatSessionMapper.ToDocument(session);
            if (unreadBySession.TryGetValue(session.SessionId, out var unread))
            {
                foreach (var (userId, count) in unread)
                {
                    // 未读补算：优先命中已有成员状态片段，缺失则新建，避免覆盖置顶/免打扰。
                    var state = doc.MemberStates.FirstOrDefault(s => s.MemberId == userId)
                                ?? new ChatSessionMemberStateDocument { MemberId = userId };
                    state.UnreadCount = count;
                    if (!doc.MemberStates.Any(s => s.MemberId == userId))
                        doc.MemberStates.Add(state);
                }
            }

            // 幂等：以 _id=SessionId upsert，可重跑不产生重复。
            var filter = Builders<ChatSessionDocument>.Filter.Eq(d => d.SessionId, doc.SessionId);
            await _sessions.ReplaceOneAsync(filter, doc,
                new ReplaceOptions { IsUpsert = true }, cancellationToken);
            migrated++;
        }

        return migrated;
    }

    /// <summary>分页迁移消息文档（含附件），幂等 upsert。</summary>
    private async Task<int> MigrateMessagesAsync(CancellationToken cancellationToken)
    {
        var migrated = 0;
        var page = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var messages = await context.Messages.AsNoTracking()
                .Include(m => m.Attachments)
                .OrderBy(m => m.SentTime)
                .Skip(page * BatchSize)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
                break;

            var writes = messages
                .Select(m => (WriteModel<ChatMessageDocument>)new ReplaceOneModel<ChatMessageDocument>(
                    Builders<ChatMessageDocument>.Filter.Eq(d => d.MessageId, m.MessageId),
                    ChatMessageMapper.ToDocument(m))
                {
                    IsUpsert = true
                })
                .ToList();

            await _messages.BulkWriteAsync(writes,
                new BulkWriteOptions { IsOrdered = false }, cancellationToken);
            migrated += messages.Count;

            if (messages.Count < BatchSize)
                break;
            page++;
        }

        return migrated;
    }
}

/// <summary>存量迁移的统计结果。</summary>
public sealed class MigrationResult
{
    /// <summary>已迁移（写入）的会话数。</summary>
    public int SessionsMigrated { get; init; }

    /// <summary>已迁移（写入）的消息数。</summary>
    public int MessagesMigrated { get; init; }
}