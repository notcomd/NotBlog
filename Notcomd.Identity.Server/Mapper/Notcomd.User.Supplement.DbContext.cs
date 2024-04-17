using Microsoft.EntityFrameworkCore;

using Notcomd.Identity.Server.Module;

using System.Security.Cryptography.X509Certificates;

namespace Notcomd.Identity.Server.Mapper
{
    public class Notcomd_User_Supplement_DbContext : DbContext
    {
        
        public DbSet<Notcomd_User_Supplement_Module> Notcomd_User_Supplement_Modules { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Notcomd_User_Supplement_Module>()
                .HasMany(p => p.Notcomd_User_Modules)
                .WithOne(p => p.Notcomd_User_Supplement_Module)
                .HasPrincipalKey(p => p.WeChat_Numble)
                .HasForeignKey(p => p.Email);
        }
    }
 
}
