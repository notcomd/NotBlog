namespace Identity.Infrastructure.Idempotent;

public interface IRequestManagement
{
    Task<bool> ExecuteAsync(Guid request);

    Task CreateRequestForCommandAsync<T>(Guid request);
}