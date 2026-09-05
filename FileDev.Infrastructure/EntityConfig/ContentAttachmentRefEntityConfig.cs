using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

/// <summary>内容附件弱引用表映射（ContentRef 表）。</summary>
public class ContentAttachmentRefEntityConfig : IEntityTypeConfiguration<ContentAttachmentRef>
{
    public void Configure(EntityTypeBuilder<ContentAttachmentRef> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.ToTable("ContentAttachmentRef");

        builder.Property(x => x.ContentId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ContentType).HasConversion<int>();

        builder.Property(x => x.FileUri).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.SourceFileId).IsRequired();

        // 附件回收按 FileUri 统计活跃引用（CountActiveByFileUriAsync），必加索引
        builder.HasIndex(x => x.FileUri)
            .HasDatabaseName("IX_ContentAttachmentRef_FileUri");

        // 按业务内容注销其附件（UnregisterByContentAsync），复合索引加速
        builder.HasIndex(x => new { x.ContentId, x.ContentType })
            .HasDatabaseName("IX_ContentAttachmentRef_Content");
    }
}