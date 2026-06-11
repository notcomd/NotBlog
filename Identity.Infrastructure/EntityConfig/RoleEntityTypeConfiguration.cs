namespace Identity.Infrastructure.EntityConfig;

public class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Roles>
{
    public void Configure(EntityTypeBuilder<Roles> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.RoleGuid);

        builder.Property(r => r.RoleGuid).HasDefaultValueSql("gen_random_uuid()");

        builder.Ignore(r => r.DomainEventbus);
        builder.Ignore(r => r.UserGuid);

        builder.Property(r => r.RoleAuthority).HasConversion<string>().HasMaxLength(50);

        builder.Property(r => r.RoleStatus).HasConversion<string>().HasMaxLength(50);

        // 多对多: Roles ↔ Permission（角色直连权限）
        builder.HasMany(r => r.Permissions)
            .WithMany(p => p.Roles)
            .UsingEntity<Dictionary<string, object>>("RolePermissions",
                j => j.HasOne<Permission>().WithMany().HasForeignKey("PermissionGuid"),
                j => j.HasOne<Roles>().WithMany().HasForeignKey("RoleGuid"));

        // 多对多: Roles ↔ RoleGroup（角色归属组）
        builder.HasMany(r => r.RoleGroups)
            .WithMany(g => g.Roles)
            .UsingEntity<Dictionary<string, object>>("RoleGroups",
                j => j.HasOne<RoleGroup>().WithMany().HasForeignKey("RoleGroupGuid"),
                j => j.HasOne<Roles>().WithMany().HasForeignKey("RoleGuid"));
    }
}