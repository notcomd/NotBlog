using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Message.Infrastructure.Mongo;

/// <summary>
/// 消息/会话 MongoDB 集合的进程级装配助手（类映射 + 索引）。
/// <para>
/// 对标 <c>FileDev.Infrastructure.Mongo.MongoChunkCollection</c>：集中管理
/// <see cref="ChatMessageDocument"/>（message 集合）与 <see cref="ChatSessionDocument"/>（chat_session 集合）
/// 的文档映射与索引，进程级双检锁单例缓存集合。类型映射与索引首次访问注册一次，后续复用。
/// </para>
/// </summary>
public static class MongoChatCollection
{
    static MongoChatCollection()
    {
        // MongoDB.Driver 3.x 默认 GuidRepresentationMode=V3，BsonDefaults.GuidRepresentation 为 Unspecified，
        // 类映射 AutoMap 出的 Guid 序列化器在 LINQ 常量序列化/写入时会抛
        // "GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified"。
        // 此处显式注册 Standard（binary subtype 4，与 uuidRepresentation=standard 一致），
        // 且静态构造器先于本类任何类映射注册/查询执行，保证序列化器抢先生效。
        BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));
    }

    /// <summary>集合名：消息</summary>
    public const string MessageCollectionName = "chat_message";

    /// <summary>集合名：会话</summary>
    public const string SessionCollectionName = "chat_session";

    private static readonly object SyncRoot = new();
    private static IMongoCollection<ChatMessageDocument>? _messages;
    private static IMongoCollection<ChatSessionDocument>? _sessions;

    /// <summary>
    /// 获取（必要时装配）消息集合。
    /// <para>装配两步只执行一次：①注册 BSON 类映射（显式声明 <see cref="ChatMessageDocument.MessageId"/> 为文档主键）；
    /// ②创建查询所需复合索引。</para>
    /// </summary>
    public static IMongoCollection<ChatMessageDocument> GetMessages(IMongoDatabase database)
    {
        if (_messages is not null)
            return _messages;

        lock (SyncRoot)
        {
            if (_messages is not null)
                return _messages;

            EnsureMessageClassMapRegistered();
            var collection = database.GetCollection<ChatMessageDocument>(MessageCollectionName);
            CreateMessageIndexes(collection);
            _messages = collection;
            return _messages;
        }
    }

    /// <summary>
    /// 获取（必要时装配）会话集合。
    /// </summary>
    public static IMongoCollection<ChatSessionDocument> GetSessions(IMongoDatabase database)
    {
        if (_sessions is not null)
            return _sessions;

        lock (SyncRoot)
        {
            if (_sessions is not null)
                return _sessions;

            EnsureSessionClassMapRegistered();
            var collection = database.GetCollection<ChatSessionDocument>(SessionCollectionName);
            CreateSessionIndexes(collection);
            _sessions = collection;
            return _sessions;
        }
    }

    private static void EnsureMessageClassMapRegistered()
    {
        // ChatMessageDocument 为纯 POCO（无 BsonId 特性，保持 Domain 无 MongoDB 依赖），
        // 此处通过类映射显式声明 MessageId 为文档 _id。
        if (!BsonClassMap.IsClassMapRegistered(typeof(ChatMessageDocument)))
        {
            BsonClassMap.RegisterClassMap<ChatMessageDocument>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(d => d.MessageId);
            });
        }
    }

    private static void EnsureSessionClassMapRegistered()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(ChatSessionDocument)))
        {
            BsonClassMap.RegisterClassMap<ChatSessionDocument>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(d => d.SessionId);
            });
        }
    }

    private static void CreateMessageIndexes(IMongoCollection<ChatMessageDocument> collection)
    {
        // CreateOne 幂等：索引已存在时重名则忽略，App 多实例启动不会冲突。
        // 会话历史分页：(sessionId, sentTime desc)
        collection.Indexes.CreateOne(new CreateIndexModel<ChatMessageDocument>(
            Builders<ChatMessageDocument>.IndexKeys
                .Ascending(d => d.SessionId)
                .Descending(d => d.SentTime),
            new CreateIndexOptions { Name = "IX_ChatMessage_SessionId_SentTime" }));

        // 离线补推：(receiverId, status)
        collection.Indexes.CreateOne(new CreateIndexModel<ChatMessageDocument>(
            Builders<ChatMessageDocument>.IndexKeys
                .Ascending(d => d.ReceiverId)
                .Ascending(d => d.Status),
            new CreateIndexOptions { Name = "IX_ChatMessage_ReceiverId_Status" }));

        // 已发消息查询：(senderId, sentTime desc)
        collection.Indexes.CreateOne(new CreateIndexModel<ChatMessageDocument>(
            Builders<ChatMessageDocument>.IndexKeys
                .Ascending(d => d.SenderId)
                .Descending(d => d.SentTime),
            new CreateIndexOptions { Name = "IX_ChatMessage_SenderId_SentTime" }));
    }

    private static void CreateSessionIndexes(IMongoCollection<ChatSessionDocument> collection)
    {
        // 会话列表 / 未读总览：participants.userId 多键索引
        collection.Indexes.CreateOne(new CreateIndexModel<ChatSessionDocument>(
            Builders<ChatSessionDocument>.IndexKeys.Ascending(d => d.Participants),
            new CreateIndexOptions { Name = "IX_ChatSession_Participants" }));

        // 按类型查询
        collection.Indexes.CreateOne(new CreateIndexModel<ChatSessionDocument>(
            Builders<ChatSessionDocument>.IndexKeys.Ascending(d => d.SessionType),
            new CreateIndexOptions { Name = "IX_ChatSession_Type" }));

        // 社区频道会话查询（社区聊天 tab 打开时按 CircleId 定位会话）
        collection.Indexes.CreateOne(new CreateIndexModel<ChatSessionDocument>(
            Builders<ChatSessionDocument>.IndexKeys.Ascending(d => d.CircleId),
            new CreateIndexOptions { Name = "IX_ChatSession_CircleId" }));
    }
}