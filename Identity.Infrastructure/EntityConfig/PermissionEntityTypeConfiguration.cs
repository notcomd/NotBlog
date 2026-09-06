using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Identity.Infrastructure.EntityConfig;

public class PermissionEntityTypeConfiguration : IEntityTypeConfiguration<Permission>
{
    /// <summary>PermissionType 枚举 ↔ 小写字符串（兼容存量 'api' 值，读取忽略大小写）</summary>
    private static readonly ValueConverter<PermissionType, string> TypeConverter = new(
        v => v.ToString().ToLowerInvariant(),
        v => Enum.Parse<PermissionType>(v, ignoreCase: true));

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.Ignore(b => b.DomainEvents);
        builder.Ignore(o => o.Id);
        builder.HasKey(x => x.PermissionId);

        builder.Property(x => x.PermissionCode).IsRequired().HasMaxLength(128);
        builder.Property(x => x.PermissionName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.PermissionType)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(TypeConverter);
        builder.Property(x => x.ParentId);
        builder.Property(x => x.Url).HasMaxLength(2048);
        builder.Property(x => x.Icon).HasMaxLength(255);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);

        // 自引用树：ParentId → PermissionId（父删时子节点保留，由业务层软删/校验）
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Roles).WithMany(x => x.Permissions)
            .UsingEntity<Dictionary<string, object>>("RolePermissions",
                j => j.HasOne<Roles>()
                    .WithMany()
                    .HasForeignKey("RoleGuid"), j => j.HasOne<Permission>()
                    .WithMany()
                    .HasForeignKey("PermissionGuid"));

        builder.HasMany(p => p.RoleGroups)
            .WithMany(g => g.Permissions)
            .UsingEntity<Dictionary<string, object>>("RoleGroupPermissions",
                j => j.HasOne<RoleGroup>()
                    .WithMany()
                    .HasForeignKey("RoleGroupGuid"), j => j.HasOne<Permission>()
                    .WithMany()
                    .HasForeignKey("PermissionGuid"));
    }
}
