using Identity.Domain.AggregatesModel.Author2Aggregate;

namespace Identity.Domain.IRepository;

public interface IAuthor2Repository : IRepository<Author2>
{
    Task AddAuthor2Async(Author2 author2);

    Task<Author2?> FindAuthor2ByUserIdAsync(Guid userId);

    Task<Author2?> FindAuthor2ByProviderAsync(string loginProvider, string providerKey);

    Task<Author2?> FindAuthor2ByUserIdAndProviderAsync(Guid userId, string loginProvider);

    Task<IEnumerable<Author2>> GetAllAuthor2sAsync();
}