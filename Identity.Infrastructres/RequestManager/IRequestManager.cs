namespace Identity.Infrastructure.RequestManager
{
    public interface IRequestManager
    {
        Task<bool> ExistAsync(Guid id);

        Task CreateRequestForCommandAsync<T>(Guid clientGuid);
    }
}
