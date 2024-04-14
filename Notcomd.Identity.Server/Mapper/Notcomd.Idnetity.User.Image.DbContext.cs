using Microsoft.EntityFrameworkCore;
using Notcomd.Identity.Server.Module;
namespace Notcomd.Identity.Server.Mapper;

public class Notcomd_Identity_User_Image_DbContext:DbContext{

    public DbSet<Notcomd_User_Image_Module> notcomd_user_image_modules { get; set;}

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder){
        base.OnModelCreating(builder);
    }
}
