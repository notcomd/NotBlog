using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Configuration;

public class UserExternalLoginEntityTypeConfiguration : IEntityTypeConfiguration<Author2>
{
    public void Configure(EntityTypeBuilder<Author2> builder)
    {
        builder.ToTable("Author2");

        builder.Property(en => en.Id).UseHiLo("Author2q");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.LoginProvider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ProviderKey)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ProviderDisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.AccessToken)
            .HasMaxLength(2000);

        builder.Property(e => e.RefreshToken)
            .HasMaxLength(2000);

        builder.HasIndex(e => new { e.LoginProvider, e.ProviderKey })
            .IsUnique();

        // 配置为聚合根的一部分，不直接修改
        builder.Ignore(e => e.DomainEventbus);
    }
}
