using Microsoft.EntityFrameworkCore;

using Notcomd.Image.Server.Module;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Mapper
{
    public class Notcomd_Image_Owenrship_Module_DbContext:DbContext
    {
        public DbSet<Notcomd_Image_Ownership_Module> notcomd_Image_Owenrship_Modules { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
<<<<<<< HEAD
            modelBuilder.Entity<Notcomd_Image_Ownership_Module>().HasKey(p => new { p.Image_Ownership_UserName });
=======
            modelBuilder.Entity<Notcomd_Image_Ownership_Module>(opt =>
            {
                opt.HasKey(opt => new { opt.Image_Ownership_UserName});
               // opt.HasMany<Notcomd_Image_Ownership_Module>(opt=>opt.Image_Ownership_UserName)
            });
>>>>>>> 5e06be5c8a45237ed4ab9101f8316484780ae68c
           
        }
    }
}
