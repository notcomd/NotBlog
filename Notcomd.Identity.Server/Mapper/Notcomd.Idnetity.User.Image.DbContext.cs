using Microsoft.EntityFrameworkCore;
using Notcomd.Identity.Server.Module;
namespace Notcomd.Identity.Server.Mapper;

public class Notcomd_Identity_User_Image_DbContext:DbContext{

    public DbSet<Notcomd_User_Image_Module> notcomd_user_image_modules { get; set;}

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
    }

    protected override void OnModelCreating(ModelBuilder builder){
        base.OnModelCreating(builder);
        builder.Entity<Notcomd_User_Image_Module>(entity =>
        {
            entity.HasKey(p => p.Identity_UserName);
        });

        builder.Entity<Notcomd_User_Image_Module>()
            .HasMany(p => p.Notcomd_User_Modules)
            .WithOne(p => p.Notcomd_User_Image_Module)
            .HasForeignKey(p => p.Email)
            .HasPrincipalKey(p => p.Identity_UserName);

    }
}
