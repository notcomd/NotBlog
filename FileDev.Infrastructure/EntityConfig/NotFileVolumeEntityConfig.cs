using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

/// <summary>数据卷实体映射：存储的卷统计记录。</summary>
public class NotFileVolumeEntityConfig : IEntityTypeConfiguration<NotFileVolume>
{
    public void Configure(EntityTypeBuilder<NotFileVolume> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.ToTable("NotFileVolume");
        // 卷 ID（"v{n}" 或空串）是业务主键，不使用基类 Guid Id
        builder.Ignore(x => x.Id);
        builder.HasKey(x => x.VolumeId);

        // 按卷 ID 点查频繁，加唯一索引
        builder.HasIndex(x => x.VolumeId)
            .IsUnique()
            .HasDatabaseName("IX_NotFileVolume_VolumeId");

        // 租户专属卷查询（GetByTenantAsync）按 TenantId 过滤（空表示共享/默认卷）
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_NotFileVolume_TenantId");

        builder.Property(x => x.VolumeId).HasMaxLength(64);
        builder.Property(x => x.TenantId).HasMaxLength(128);
        builder.Property(x => x.RootPath).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Kind).HasConversion<int>();
    }
}