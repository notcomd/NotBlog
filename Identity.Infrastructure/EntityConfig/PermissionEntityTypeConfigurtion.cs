namespace Identity.Infrastructure.EntityConfig;

public class PermissionEntityTypeConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.PermissionId);
        builder.Ignore(e => e.DomainEventbus);


        builder.HasMany(en => en.Roles)
            .WithMany(r => r.Permissions)
            .UsingEntity<Dictionary<string, object>>("RolePermissions",
                f => f.HasOne<Roles>().WithMany().HasForeignKey("RoleId"),
                f => f.HasOne<Permission>().WithMany().HasForeignKey("PermissionId"));


        builder.HasMany(en => en.RoleGroups)
            .WithMany(r => r.Permissions)
            .UsingEntity<Dictionary<string, object>>("GroupPermissions",
                f => f.HasOne<RoleGroup>().WithMany().HasForeignKey("RoleGroupId"),
                f => f.HasOne<Permission>().WithMany().HasForeignKey("PermissionId"));
    }
}