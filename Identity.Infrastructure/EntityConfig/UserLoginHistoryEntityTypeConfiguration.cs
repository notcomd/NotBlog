using Identity.Domain.Entities.UserAggregate;

namespace Identity.Infrastructure.EntityConfig;

/// <summary>
/// 登录历史实体配置
/// </summary>
public class UserLoginHistoryEntityTypeConfiguration : IEntityTypeConfiguration<UserLoginHistory>
{
    public void Configure(EntityTypeBuilder<UserLoginHistory> builder)
    {
        builder.ToTable("UserLoginHistory");

        builder.Ignore(b => b.DomainEventbus);

        builder.Property(o => o.Id).UseHiLo("UserLoginHistoryseq");

        builder.HasKey(x => x.LoginGuid);

        builder.Property(x => x.LoginGuid).IsRequired();

        builder.Property(x => x.UserGuid).IsRequired();

        builder.Property(x => x.Email).IsRequired(false);

        builder.Property(x => x.LoginMessage).IsRequired(false).HasMaxLength(500);

        builder.Property(x => x.CreateDataTime).IsRequired();

        builder.OwnsOne(on => on.PhoneNumber);
    }
}