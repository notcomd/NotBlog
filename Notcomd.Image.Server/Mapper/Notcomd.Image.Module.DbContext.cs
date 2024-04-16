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
    public class Notcomd_Image_Module_DbContext:DbContext
    {
        public DbSet<Notcomd_Image_Module> DbSet { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
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
                options.HasOne(po => po.Notcomd_Image_Ownership_Module)
                .WithMany(p => p.Notcomd_Image_Module)
                .HasForeignKey(sc => sc.Image_Url)
                .HasPrincipalKey(sc => sc.Image_Ownership_UserName);

                options.HasOne(po => po.Notcomd_Image_Evaluate_Module)
                .WithMany(p => p.Notcomd_Image_Module)
                .HasForeignKey(sc => sc.Image_Url)
                .HasPrincipalKey(sc => sc.Image_Ownership);

            });
        }
    }
}
