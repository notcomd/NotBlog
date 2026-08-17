namespace Identity.Infrastructure.EntityConfig;

public class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Roles>
{
    public void Configure(EntityTypeBuilder<Roles> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.RoleGuid);

        builder.Property(r => r.RoleGuid).HasDefaultValueSql("gen_random_uuid()");

        builder.Ignore(r => r.DomainEvents);
       // builder.Ignore(r => r.UserGuid);
        builder.Ignore(o=>o.Id);
        builder.Property(r => r.RoleAuthority).HasConversion<string>().HasMaxLength(50);

        builder.Property(r => r.RoleStatus).HasConversion<string>().HasMaxLength(50);

        // 多对多: Roles ↔ Permission（角色直连权限）
        builder.HasMany(r => r.Permissions)
            .WithMany(p => p.Roles)
            .UsingEntity<Dictionary<string, object>>("RolePermissions",
                j => j.HasOne<Permission>().WithMany().HasForeignKey("PermissionGuid"),
                j => j.HasOne<Roles>().WithMany().HasForeignKey("RoleGuid"));

        // Guid 列表引用 RoleGroup（通过 ID 引用，不再持有对象引用，避免双向循环依赖）
        builder.Property(r => r.RoleGroupGuids)
            .HasColumnName("role_group_guids")
            .HasColumnType("uuid[]");
    }
}
