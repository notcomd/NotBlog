using Microsoft.EntityFrameworkCore;

using Notcomd.Document.Server.Module;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Document.Server.Mapper
{
    internal class Notcomd_Document_DbContext:DbContext
    {
        public DbSet<Notcomd_Document_Module> notcomd_Document_Modules { get; set; }
        public DbSet<Notcomd_Document_Data_Module> notcomd_Document_Data_Modules { get; set; }
        public DbSet<Notcomd_Document_Ownership_Module> notcomd_Document_Ownership_Modules { get; set; }
        public DbSet<Notcomd_Document_Comment_Module> notcomd_Document_Comment_Modules { get; set; }



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
        }
    }
}
