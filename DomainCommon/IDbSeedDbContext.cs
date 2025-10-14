using Microsoft.EntityFrameworkCore;

namespace DomainCommon;

public interface IDbSeedDbContext<in TDbContext> where TDbContext : DbContext
{
    Task DbSeedAsync(TDbContext dbContext);
}
