using Microsoft.EntityFrameworkCore;

using Notcomd.Image.Server.Module;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Mapper
{
    public class Notcomd_Image_Evaluate_Module_DbContext:DbContext
    {
        public DbSet<Notcomd_Image_Evaluate_Module> Notcomd_Image_Evaluate_Modules { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Notcomd_Image_Evaluate_Module>().HasKey(p => new { p.Image_Ownership });
        }
    }
}
