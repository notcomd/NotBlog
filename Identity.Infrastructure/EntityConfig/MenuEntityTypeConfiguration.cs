using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Identity.Infrastructure.EntityConfig;

public class MenuEntityTypeConfiguration : IEntityTypeConfiguration<Menu>
{
    /// <summary>MenuType 枚举 ↔ 小写字符串（读取忽略大小写）</summary>
    private static readonly ValueConverter<MenuType, string> TypeConverter = new(
        v => v.ToString().ToLowerInvariant(),
        v => Enum.Parse<MenuType>(v, ignoreCase: true));

    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("Menus");
        builder.Ignore(b => b.DomainEvents);
        builder.Ignore(o => o.Id);
        builder.HasKey(x => x.MenuId);

        builder.Property(x => x.MenuName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.MenuType)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(TypeConverter);
        builder.Property(x => x.ParentId);
        builder.Property(x => x.Url).HasMaxLength(2048);
        builder.Property(x => x.Icon).HasMaxLength(64);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsEnabled).HasDefaultValue(true);
        builder.Property(x => x.RequiredPermissionCode).HasMaxLength(128);
        builder.Property(x => x.RequiredRole).HasMaxLength(256);

        // 自引用树：ParentId → MenuId（父删时子节点保留，由业务层软删/校验）
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentId);
    }
}