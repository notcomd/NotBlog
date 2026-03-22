namespace FileDev.Infrastructure.Idempotent;

public interface IRequestManagement
{
    Task ExecuteAsync(ClientRequest request);

    Task CreateRequestForCommandAsync(ClientRequest request);
}