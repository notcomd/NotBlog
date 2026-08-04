using Identity.Infrastructure.EntityFramework;

namespace Identity.Web.API.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse>(
    ILogger<TransactionBehavior<TRequest, TResponse>> logger,
    IdentityDbContext identityDbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IdentityDbContext _identityDbContext =
        identityDbContext ?? throw new ArgumentNullException(nameof(identityDbContext));


    public async Task<TResponse> Handler(TRequest request, Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var response = default(TResponse);
        var typeName = request.GetGenericTypeName();
        try
        {
            if (_identityDbContext.HasActiveTransaction)
            {
                return await next();
            }

            var strategy = _identityDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _identityDbContext.BeginTransactionAsync();
                using (_logger.BeginScope(new List<KeyValuePair<string, object>>
                       {
                           new("TransactionId", transaction.TransactionId)
                       }))
                {
                    // S-16：不记录命令对象（可能含敏感信息），仅记录事务与命令名
                    _logger.LogInformation("Begin transaction {TransactionId} for {CommandName}",
                        transaction.TransactionId, typeName);

                    response = await next();

                    _logger.LogInformation("Commit transaction {TransactionId} for {CommandName}",
                        transaction.TransactionId, typeName);

                    await _identityDbContext.CommitTransactionAsync(transaction);
                }
            });
            return response!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transaction for {CommandName}", typeName);
            throw;
        }
    }
}