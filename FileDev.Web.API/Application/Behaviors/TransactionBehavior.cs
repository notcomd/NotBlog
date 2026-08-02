using Microsoft.EntityFrameworkCore;
using Notcomd.EventBus.Core;
using NotMediator;

namespace FileDev.Web.API.ActionFilter.Behaviors;

public class TransactionBehavior<TRequest, TResponse>(
    ILogger<TransactionBehavior<TRequest, TResponse>> logger,
    NotFileDbContext notFileDbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly NotFileDbContext _notFileDbContext =
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
                Guid transactionId;
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

                    transactionId = transaction.TransactionId;
                }
            });
            return response!;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in transaction {TransactionId} for {CommandName} ({@Command})",
                e, typeName, request);
            throw;
        }
    }
}