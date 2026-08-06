namespace Markdown.Web.API.Application.Behaviors;

public class LoggerBehavior<TRequest, TResponse>(ILogger<LoggerBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggerBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));


    public async Task<TResponse> Handler(TRequest request, Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        // 只记录命令类型名，不记录请求/响应完整对象：
        // 命令体与响应都可能含 1MB 级 MarkdownContent / 整页列表，落日志会导致日志膨胀与敏感内容外泄（P2-5）
        _logger.LogInformation("Handling Command {CommandName}:", request.GetGenericTypeName());
        var response = await next();
        _logger.LogInformation("Handled Command {CommandName}", request.GetGenericTypeName());
        return response;
    }
}