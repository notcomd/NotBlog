

using Microsoft.EntityFrameworkCore;

namespace DomainCommonst;

public interface IDbSeedDbContext<in TDbContext> where TDbContext : DbContext
{
    Task DbSeedAsync(TDbContext dbContext);
}
