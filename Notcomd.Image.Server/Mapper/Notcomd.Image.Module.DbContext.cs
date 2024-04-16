using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

        /// <summary>
        /// 设置表关联
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Notcomd_Image_Module>(options =>
            {
                options.HasKey(options => new { options.Image_Url });
                options.HasOne(po => po.notcomd_Image_Ownership_Module)
                .WithMany(p => p.notcomd_Image_Modules)
                .HasForeignKey(sc => sc.notcomd_Image_Ownership_Module);
                
                options.HasOne(po => po.notcomd_Image_Evaluate_Module)
                .WithMany(p => p.notcomd_Image_Modules)
                .HasForeignKey(sc => sc.notcomd_Image_Evaluate_Module);

            });
        }
    }
}
