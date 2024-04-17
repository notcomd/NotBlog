using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Notcomd.Identity.Server.Module;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Mapper
{
    public class Notcomd_Identity_DbContext : IdentityDbContext<Notcomd_User_Module,Notcomd_Role_Module,long>
    {
        public DbSet<Notcomd_Role_Module> notcomd_Role_Modules { get; set; }

        public DbSet<Notcomd_User_Module> notcomd_User_Modules { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Notcomd_User_Module>()
                .HasOne(p => p.Notcomd_User_Image_Module)
                .WithMany(ps => ps.Notcomd_User_Modules)
                .HasForeignKey(p => p.Email)
                .HasPrincipalKey(p=>p.Identity_UserName);
            modelBuilder.Entity<Notcomd_User_Module>()
                .HasOne(po => po.Notcomd_User_Supplement_Module)
                .WithMany(p => p.Notcomd_User_Modules)
                .HasForeignKey(p=>p.Email)
                .HasPrincipalKey(p=>p.WeChat_Numble);
                
        }
    }
}
