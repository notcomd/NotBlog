public class PermissionEntityTypeConfigurtion : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.Ignore(b => b.DomainEvents);
        builder.Ignore(o=>o.Id);        
        builder.HasKey(x => x.PermissionId);


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