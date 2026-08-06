namespace Markdown.Infrastructure.Idempotent;

public class ClientRequest
{
    public Guid ClientRequestId { get; set; }
    public string ClientRequestName { get; set; } = null!;
    public DateTimeOffset Created { get; set; }

    /// <summary>
    ///     命令首次执行成功后的响应（JSON 序列化），供相同 IdempotencyKey 的重复请求直接返回；
    ///     占位阶段为 null，命令执行成功后由 UpdateResponseAsync 写入
    /// </summary>
    public string? ResponseJson { get; set; }
}
