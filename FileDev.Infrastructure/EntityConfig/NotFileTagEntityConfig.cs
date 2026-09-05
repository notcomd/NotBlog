using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class NotFileTagEntityConfiguration : IEntityTypeConfiguration<NotFileTag>
{
    public void Configure(EntityTypeBuilder<NotFileTag> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.ToTable("NotFileTag");
        builder.Ignore(x => x.Id);
        builder.HasKey(x => x.TagId);

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_NotFileTag_UserId");

        // 用户内标签名唯一（含未删除约束），支撑"覆盖新建/改名防重"的幂等语义。
        builder.HasIndex(x => new { x.UserId, x.TagName })
            .HasDatabaseName("IX_NotFileTag_UserId_TagName")
            .IsUnique();

        // 按默认标签种类归类（上传自动归类按 用户+种类 查找目标标签）。
        builder.HasIndex(x => new { x.UserId, x.DefaultKind })
            .HasDatabaseName("IX_NotFileTag_UserId_DefaultKind");

        builder.Property(x => x.TagName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TagDescription).HasMaxLength(500).IsRequired(false);

        builder.Property(x => x.FileIds).HasConversion(
                v => string.Join(",", v.Distinct().OrderBy(x => x)),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList(),
                new ValueComparer<List<Guid>>(
                    (l, r) => l!.SequenceEqual(r!),
                    v => v.Aggregate(0, (a, x) => HashCode.Combine(a, x.GetHashCode())),
                    v => new List<Guid>(v)))
            .HasMaxLength(-1);

        builder.Property(x => x.DefaultKind)
            .IsRequired()
            .HasDefaultValue(FileDev.Domain.Enum.TagDefaultKind.None);

        builder.Property(x => x.UploadTime).IsRequired();
        builder.Property(x => x.UpdateTime).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeleteTime);
    }
}