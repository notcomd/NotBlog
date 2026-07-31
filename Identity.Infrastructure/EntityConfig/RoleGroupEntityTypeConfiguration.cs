namespace Identity.Infrastructure.EntityConfig;

public class RoleGroupEntityTypeConfiguration : IEntityTypeConfiguration<RoleGroup>
{
    public void Configure(EntityTypeBuilder<RoleGroup> builder)
    {
        builder.ToTable("RoleGroups");

        builder.HasKey(g => g.RoleGroupGuid);

        builder.Property(g => g.RoleGroupGuid).HasDefaultValueSql("gen_random_uuid()");

        builder.Ignore(g => g.DomainEventbus);

        // Guid 列表引用 Roles（通过 ID 引用，不再持有对象引用，避免双向循环依赖）
        builder.Property(g => g.RoleGuids)
            .HasColumnName("role_guids")
            .HasColumnType("uuid[]");

        // 多对多: RoleGroup ↔ Permission（组拥有权限）
        builder.HasMany(g => g.Permissions)
            .WithMany(p => p.RoleGroups)
            .UsingEntity<Dictionary<string, object>>("GroupPermissions",
                j => j.HasOne<Permission>().WithMany().HasForeignKey("PermissionGuid"),
                j => j.HasOne<RoleGroup>().WithMany().HasForeignKey("RoleGroupGuid"));
    }
}
