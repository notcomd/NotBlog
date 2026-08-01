namespace Message.Web.API.Grpc;

/// <summary>
/// 文件存储 gRPC 客户端配置选项。
/// 正常运行时优先通过 Aspire 服务发现解析 FileDev 服务地址（服务名：filedev-web-api），
/// 独立启动（脱离 AppHost）时使用 <see cref="Address"/> 作为兜底地址。
/// </summary>
public class FileStorageGrpcOptions
{
    /// <summary>appsettings.json 中的配置节名称</summary>
    public const string SectionName = "FileStorageGrpc";

    /// <summary>
    /// FileDev.Web.API 的 gRPC 服务地址（兜底地址）。
    /// 默认值对应 FileDev.Web.API 本地开发端口（https）。
    /// </summary>
    public string Address { get; set; } = "https://localhost:9093";

    /// <summary>gRPC 单条消息允许的最大尺寸（MB），需与 FileDev 服务端配置保持一致</summary>
    public int MaxMessageSizeMb { get; set; } = 1024;

    /// <summary>单个分片建议大小（字节），默认 5MB</summary>
    public int ChunkSize { get; set; } = 5 * 1024 * 1024;

    /// <summary>瞬时故障（网络抖动/服务不可用）时的最大重试次数</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>单次 gRPC 调用超时（秒）</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
