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
<<<<<<< HEAD

=======
<<<<<<< HEAD
>>>>>>> 1bc8c836d3b77d793591b5ce2d586d5d7e18079f
                options.HasOne(po => po.Notcomd_Image_Ownership_Module)
                .WithMany(p => p.Notcomd_Image_Module)
                .HasForeignKey(sc => sc.Image_Url)
                .HasPrincipalKey(sc => sc.Image_Ownership_UserName);

                options.HasOne(po => po.Notcomd_Image_Evaluate_Module)
                .WithMany(p => p.Notcomd_Image_Module)
                .HasForeignKey(sc => sc.Image_Url)
                .HasPrincipalKey(sc => sc.Image_Ownership);
<<<<<<< HEAD

                //options.HasOne(po => po.Notcomd_Image_Ownership_Module)
                //.WithMany(p => p.Notcomd_Image_Module)
                //.HasForeignKey(sc => sc.Notcomd_Image_Ownership_Module);
                
                //options.HasOne(po => po.Notcomd_Image_Evaluate_Module)
                //.WithMany(p => p.Notcomd_Image_Module)
                //.HasForeignKey(sc => sc.Notcomd_Image_Evaluate_Module);
=======
=======
                options.HasOne(po => po.notcomd_Image_Ownership_Module)
                .WithMany(p => p.notcomd_Image_Modules)
                .HasForeignKey(sc => sc.notcomd_Image_Ownership_Module);
                
                options.HasOne(po => po.notcomd_Image_Evaluate_Module)
                .WithMany(p => p.notcomd_Image_Modules)
                .HasForeignKey(sc => sc.notcomd_Image_Evaluate_Module);
>>>>>>> 5e06be5c8a45237ed4ab9101f8316484780ae68c
>>>>>>> 1bc8c836d3b77d793591b5ce2d586d5d7e18079f

            });
        }
    }
}
