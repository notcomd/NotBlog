using Microsoft.EntityFrameworkCore;

using Notcomd.Document.Server.Module;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Document.Server.Mapper
{
    public  class Notcomd_Document_Data_DbContext:DbContext
    {
        public DbSet<Notcomd_Document_Data_Module> notcomd_Document_Data_Modules { get; set; }
        public Notcomd_Document_Data_DbContext(DbContextOptions<Notcomd_Document_Data_DbContext> dbContextOptions ):base(dbContextOptions) 
        {

        }

        public Notcomd_Document_Data_DbContext() { }


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
