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
        _logger.LogInformation("Handling Command {CommandName} {@Command}:", request.GetGenericTypeName(), request);
        var response = await next();
        _logger.LogInformation("d Command {CommandName}with response {@Response}",
            request.GetGenericTypeName(), response);
        return response;
    }
}