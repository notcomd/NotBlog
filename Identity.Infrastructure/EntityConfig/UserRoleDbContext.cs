using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.EntityConfig;

public class UserRoleDbContext: DbContext
{
    public DbSet<UserRole> UserRoles { get; set; }
    
    public UserRoleDbContext(DbContextOptions<UserRoleDbContext> options):base(options){}
    
}