using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class NotFileEntityConfiguration: IEntityTypeConfiguration<NotFile>
{
    public void Configure(EntityTypeBuilder<NotFile> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.ToTable("NotFile");
        //builder.Property(x => x.Id).UseHiLo("NotFileSeq");
        builder.Ignore(x=>x.Id);
        builder.HasKey(xn => xn.FileId);

        // FileId 是业务主键（GUID），必须唯一且常用于点查，添加唯一索引
        builder.HasIndex(x => x.FileId)
            .IsUnique()
            .HasDatabaseName("IX_NotFile_FileId");

        // UserId 用于按用户查询文件列表、统计数量、计算配额等，添加索引加速
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_NotFile_UserId");

        // 秒传查询（GetDeduplicateFileAsync）按 FileMd5 + FileSize 过滤，添加复合索引
        builder.HasIndex(x => new { x.FileMd5, x.FileSize })
            .HasDatabaseName("IX_NotFile_FileMd5_FileSize");

        // 物理文件引用计数（CountActiveRefsByFileUriAsync）按 FileUri 过滤，添加索引
        builder.HasIndex(x => x.FileUri)
            .HasDatabaseName("IX_NotFile_FileUri");

        // 软删除过滤在多个查询中使用，添加 IsDeleted 索引（与 UserId 组合更高效）
        builder.HasIndex(x => new { x.UserId, x.IsDeleted })
            .HasDatabaseName("IX_NotFile_UserId_IsDeleted");

        // 字符串列设置最大长度，防止无限制存储
        builder.Property(x => x.FileName).HasMaxLength(256);
        builder.Property(x => x.FileDescription).HasMaxLength(2000);
        builder.Property(x => x.FileMd5).HasMaxLength(128);
        builder.Property(x => x.FileUri).HasMaxLength(1024);
    }
}
