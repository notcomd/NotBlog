using FileDev.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace FileDev.Infrastructure.Mongo;

/// <summary>
/// 分片上传记录集合的进程级装配助手（类映射 + 唯一索引）。
/// <para>
/// 集中管理 <see cref="FileChunkRecord"/> 在 MongoDB 中的文档映射与索引，
/// 避免每个仓储实例重复注册。类型映射与索引在首次访问时注册一次，后续复用。
/// </para>
/// </summary>
public static class MongoChunkCollection
{
    static MongoChunkCollection()
    {
        // MongoDB.Driver 3.x 默认 GuidRepresentationMode=V3，BsonDefaults.GuidRepresentation 为 Unspecified，
        // 类映射 AutoMap 出的 Guid 序列化器在写/查（含 LINQ 常量序列化）时会抛
        // "GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified"。
        // 显式注册 Standard（binary subtype 4，与 uuidRepresentation=standard 一致），
        // 静态构造器先于本类任何类映射注册执行，保证序列化器抢先生效。
        BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));
    }

    /// <summary>集合名：文件分片上传记录</summary>
    public const string CollectionName = "file_chunk_records";

    /// <summary>唯一索引名：以 FileKey（业务唯一键）为文档唯一约束，支撑幂等初始化。</summary>
    public const string UniqueFileKeyIndexName = "UX_FileChunkRecord_FileKey";

    private static readonly object SyncRoot = new();

    private static IMongoCollection<FileChunkRecord>? _collection;

    /// <summary>
    /// 获取（必要时装配）分片上传记录集合。
    /// <para>两次装配步骤只执行一次：①注册 BSON 类映射（自动映射 + 显式声明 <c>RecordId</c> 为文档主键）；
    /// ②为 <c>FileKey</c> 创建唯一索引，使 <see cref="MongoFileChunkRepository.InsertAsync"/> 的幂等冲突处理有约束可依。
    /// </para>
    /// </summary>
    public static IMongoCollection<FileChunkRecord> Get(IMongoDatabase database)
    {
        if (_collection is not null)
            return _collection;

        lock (SyncRoot)
        {
            if (_collection is not null)
                return _collection;

            EnsureClassMapRegistered();

            var collection = database.GetCollection<FileChunkRecord>(CollectionName);
            CreateUniqueFileKeyIndex(collection);

            _collection = collection;
            return _collection;
        }
    }

    private static void EnsureClassMapRegistered()
    {
        // FileChunkRecord 为纯 POCO（无 BsonId 特性，保持 Domain 无 MongoDB 依赖），
        // 此处通过类映射显式声明 RecordId 为文档 _id。
        if (!BsonClassMap.IsClassMapRegistered(typeof(FileChunkRecord)))
        {
            BsonClassMap.RegisterClassMap<FileChunkRecord>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(c => c.RecordId);
            });
        }
    }

    private static void CreateUniqueFileKeyIndex(IMongoCollection<FileChunkRecord> collection)
    {
        // CreateOne 幂等：索引已存在时重名则忽略，App 多实例启动不会冲突。
        collection.Indexes.CreateOne(new CreateIndexModel<FileChunkRecord>(
            Builders<FileChunkRecord>.IndexKeys.Ascending(c => c.FileKey),
            new CreateIndexOptions { Unique = true, Name = UniqueFileKeyIndexName }));
    }
}