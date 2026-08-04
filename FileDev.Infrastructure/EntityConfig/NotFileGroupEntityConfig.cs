using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class NotFileGroupEntityConfiguration : IEntityTypeConfiguration<NotFileGroup>
{
    public void Configure(EntityTypeBuilder<NotFileGroup> builder)
    {
        builder.Ignore(en => en.DomainEventBus);
        builder.ToTable("NotFileGroup");
        builder.Property(x => x.Id).UseHiLo("NotFileGroupseq");
        builder.HasKey(x => x.Id);

        // 自引用树形关系 — 使用 NotFileGroupId (Guid) 作为主键端
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentGroupId)
            .HasPrincipalKey(x => x.NotFileGroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // UserId 用于按用户查询文件组列表，添加索引加速
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_NotFileGroup_UserId");

        // 同级重名校验（ExistsByNameAtSameLevelAsync）按 UserId + ParentGroupId + FileGroupName 查询，
        // 添加复合索引加速唯一性检查
        builder.HasIndex(x => new { x.UserId, x.ParentGroupId, x.FileGroupName })
            .HasDatabaseName("IX_NotFileGroup_UserId_ParentGroupId_FileGroupName");

        // 字符串列设置最大长度
        builder.Property(x => x.FileGroupName).HasMaxLength(256);

        // HashSet 集合映射为 JSON 列
        builder.Property(x => x.FileIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<Guid>())
            .HasColumnType("text");

        builder.Property(x => x.FileGroupTags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<string>())
            .HasColumnType("text");
    }
}
