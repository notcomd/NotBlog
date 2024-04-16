using Microsoft.EntityFrameworkCore;

using Notcomd.Image.Server.Module;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Mapper
{
    public class Notcomd_Image_Module_DCbontext:DbContext
    {
        public DbSet<Notcomd_Image_Module> DbSet { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
