namespace FileDev.Infrastructure.Idempotent;

public class RequestManagement : IRequestManagement
{
    public async Task ExecuteAsync(ClientRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task CreateRequestForCommandAsync(ClientRequest request)
    {
        throw new NotImplementedException();
    }
}