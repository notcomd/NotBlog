namespace Identity.Infrastructure.RequestManager
{
    public class RequestManager : IRequestManager
    {

        private readonly IdentityDbContext identityDbContext;

        public RequestManager(IdentityDbContext identityDbContext)
        {
            this.identityDbContext = identityDbContext ?? throw new ArgumentNullException(nameof(identityDbContext));
        }

        public async Task<bool> ExistAsync(Guid id)
        {
            var request = await identityDbContext.FindAsync<ClientRequest>(id);
            return request != null;
        }

        public async Task CreateRequestForCommandAsync<T>(Guid id)
        {
            var exit = await ExistAsync(id);
            var request = exit ? throw new ArgumentException(nameof(id), "Request already exists") :
                new ClientRequest
                {
                    Id = id,
                    Name = typeof(T).Name,
                    Time = DateTime.UtcNow
                };
            await identityDbContext.AddAsync(request);
            await identityDbContext.SaveChangesAsync();
        }
    }
}
