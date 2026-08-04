namespace Markdown.Infrastructure.Configuration;

public class MarkDownEntityConfiguration : IEntityTypeConfiguration<MarkDown>
{
    public void Configure(EntityTypeBuilder<MarkDown> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.ToTable("MarkDown");

        // 审核状态（F-10.2）：默认值=审核通过，保证迁移后已有公开文章继续对外可见；新文章由实体默认草稿
        builder.Property(x => x.Status)
            .HasDefaultValue(MarkStatus.MarkApproved);
        // 资源限制（P-05）：正文上限 1,000,000 字符
        builder.Property(x => x.MarkDownContent)
            .HasMaxLength(1_000_000);
        builder.Property(x => x.Id).UseHiLo("MarkDownGuid");
        builder.HasKey(x => x.Id);

        builder.HasMany(en => en.MarkReviews)
            .WithOne(en => en.MarkDown)
            .HasForeignKey(en => en.MarkDownGuid)
            .HasPrincipalKey(en => en.MarkDownGuid);

        builder.HasMany(en => en.OldMarkDowns)
            .WithOne(en => en.MarkDown)
            .HasForeignKey(en => en.MarkDownGuid)
            .HasPrincipalKey(en => en.MarkDownGuid);

        // HashSet 集合映射为 JSON 列
        builder.Property(x => x.MarkDownTagboard)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<string>())
            .HasColumnType("text");
    }
}