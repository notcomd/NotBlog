namespace Video.Domain.SeedWork;

public interface IUnitOfWork : IDisposable
{
    Task<int> SavaChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> SavaEntitiesAsync(CancellationToken cancellationToken = default);
}