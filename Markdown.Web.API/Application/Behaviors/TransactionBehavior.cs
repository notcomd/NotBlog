using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse>(
    ILogger<TransactionBehavior<TRequest, TResponse>> logger,
    MarkDownDbContext markDownDbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly MarkDownDbContext _markDownDbContext =
        markDownDbContext ?? throw new ArgumentNullException(nameof(markDownDbContext));


    public async Task<TResponse> Handler(TRequest request, Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var response = default(TResponse)!;
        var typeName = request.GetGenericTypeName();
        try
        {
            if (_markDownDbContext.HasActiveTransaction)
            {
                return await next();
            }

            var strategy = _markDownDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _markDownDbContext.BeginTransactionAsync();
                using (_logger.BeginScope(new List<KeyValuePair<string, object>>
                       {
                           new("TransactionId", transaction.TransactionId)
                       }))
                {
                    // 仅记录命令类型名，不记录 {@Command} 完整对象（命令体可能含 1MB 级正文）
                    _logger.LogInformation("Begin transaction {TransactionId} for {CommandName}",
                        transaction.TransactionId, typeName);

                    response = await next();

                    _logger.LogInformation("Commit transaction {TransactionId} for {CommandName}",
                        transaction.TransactionId, typeName);

                    await _markDownDbContext.CommitTransactionAsync(transaction);
                }
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transaction for {CommandName}", typeName);
            throw;
        }
    }
}