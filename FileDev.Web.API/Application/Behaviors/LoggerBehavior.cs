using Notcomd.EventBus.Core;
using NotMediator;

namespace FileDev.Web.API.ActionFilter.Behaviors;

public class LoggerBehavior<TRequest, TResponse>(ILogger<LoggerBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggerBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TResponse> Handler(TRequest request, Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        // Major：原代码使用 {@Command} 序列化整个请求对象，会把 UploadChunkCommand.ChunkContent
        // (byte[], 单分片最高 10MB) / 流式上传内容 / 文件元数据等敏感/大对象写入日志，
        // 既泄漏敏感信息也严重拖慢日志性能。仅记录命令类型名（不序列化请求体）。
        var typeName = request.GetGenericTypeName();
        _logger.LogInformation("Handling Command {CommandName}", typeName);
        var response = await next();
        // Major：响应同样可能携带文件内容/敏感字段，仅记录类型与完成事件
        _logger.LogInformation("Handled Command {CommandName}", typeName);
        return response;
    }
}
