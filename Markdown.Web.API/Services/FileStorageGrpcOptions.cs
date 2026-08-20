namespace Markdown.Web.API.Services;

/// <summary>
///     FileDev gRPC 客户端配置（Markdown 服务版）。
///     正常运行时优先通过 Aspire 服务发现解析 FileDev 服务地址（服务名 filedev-web-api）；
///     脱离 AppHost 独立启动时使用 <see cref="Address"/> 作为兜底地址。
/// </summary>
public class FileStorageGrpcOptions
{
    /// <summary>appsettings.json 中的配置节名称</summary>
    public const string SectionName = "FileStorageGrpc";

    /// <summary>FileDev.Web.API 的 gRPC 服务地址（兜底地址，空则走服务发现）</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>瞬时故障最大重试次数</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>单次 gRPC 调用超时（秒）</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
