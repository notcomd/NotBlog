using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Mapper
{
    public class Notcomd_Image_Owenrship_Module:DbContext
    {
        public DbSet<Notcomd_Image_Owenrship_Module> notcomd_Image_Owenrship_Modules { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Notcomd_Image_Owenrship_Module>(opt =>
            {
                opt.HasKey(opt=>new {opt.})
            });
        }
    }
}
