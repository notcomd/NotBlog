using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

/// <summary>
/// 用户存储额度表配置：每用户一条记录，以 UserId 为业务主键（非 EF 的 Id）。
/// </summary>
public class UserFileInfoEntityConfig : IEntityTypeConfiguration<UserFileInfo>
{
    public void Configure(EntityTypeBuilder<UserFileInfo> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.ToTable("UserFileInfo");
        builder.Ignore(x => x.Id);
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.TotalQuotaBytes).IsRequired();
        builder.Property(x => x.UsedBytes).IsRequired();
        builder.Property(x => x.UpdateTime).IsRequired();
    }
}
