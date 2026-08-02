namespace Identity.Web.API.Application.Behaviors;

public class LoggerBehavior<TRequest, TResponse>(ILogger<LoggerBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggerBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));


    public async Task<TResponse> Handler(TRequest request, Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        // S-16：不记录命令对象（可能含明文密码/验证码等敏感信息），仅记录命令名与响应类型
        _logger.LogInformation("Handling Command {CommandName}:", request.GetGenericTypeName());
        var response = await next();
        _logger.LogInformation("Handled Command {CommandName} with response type {ResponseType}",
            request.GetGenericTypeName(), response?.GetType().Name);
        return response;
    }
}