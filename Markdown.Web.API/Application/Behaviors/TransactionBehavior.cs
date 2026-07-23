using Markdown.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Notcomd.EventBus.Core;
using NotMediator;

namespace Markdown.Web.API.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse>(
    ILogger<TransactionBehavior<TRequest, TResponse>> logger,
    MarkDownDbContext notFileDbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly MarkDownDbContext _notFileDbContext =
        notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));


    public async Task<TResponse> Handler(TRequest request, Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var response = default(TResponse);
        var typeName = request.GetGenericTypeName();
        try
        {
            if (_notFileDbContext.HasActiveTransaction)
            {
                return await next();
            }

            var strategy = _notFileDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _notFileDbContext.BeginTransactionAsync();
                using (_logger.BeginScope(new List<KeyValuePair<string, object>>
                       {
                           new("TransactionId", transaction.TransactionId)
                       }))
                {
                    _logger.LogInformation("Begin transaction {TransactionId} for {CommandName} ({@Command})",
                        transaction.TransactionId, typeName, request);

                    response = await next();

                    _logger.LogInformation("Commit transaction {TransactionId} for {CommandName}",
                        transaction.TransactionId, typeName);

                    await _notFileDbContext.CommitTransactionAsync(transaction);
                }
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transaction {TransactionId} for {CommandName} ({@Command})",
                ex, typeName, request);
            throw;
        }
    }
}