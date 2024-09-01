using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.EntityConfig;

public class UserDdContext: DbContext
{
    public DbSet<User> Users { get; set; }
    
    public UserDdContext(DbContextOptions<UserDdContext> options):base(options){}
    
    
}