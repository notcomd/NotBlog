namespace Identity.Infrastructure.EntityConfig;

public class RoleGroupEntityTypeConfiguration : IEntityTypeConfiguration<RoleGroup>
{
    public void Configure(EntityTypeBuilder<RoleGroup> builder)
    {
        builder.ToTable("RoleGroups");

        builder.HasKey(g => g.RoleGroupGuid);

        builder.Property(g => g.RoleGroupGuid).HasDefaultValueSql("gen_random_uuid()");

        builder.Ignore(g => g.DomainEventbus);

        // 多对多: RoleGroup ↔ Roles（组包含角色）
        builder.HasMany(g => g.Roles)
            .WithMany(r => r.RoleGroups)
            .UsingEntity<Dictionary<string, object>>("RoleGroups",
                j => j.HasOne<Roles>().WithMany().HasForeignKey("RoleGuid"),
                j => j.HasOne<RoleGroup>().WithMany().HasForeignKey("RoleGroupGuid"));

        // 多对多: RoleGroup ↔ Permission（组拥有权限）
        builder.HasMany(g => g.Permissions)
            .WithMany(p => p.RoleGroups)
            .UsingEntity<Dictionary<string, object>>("GroupPermissions",
                j => j.HasOne<Permission>().WithMany().HasForeignKey("PermissionGuid"),
                j => j.HasOne<RoleGroup>().WithMany().HasForeignKey("RoleGroupGuid"));
    }
}