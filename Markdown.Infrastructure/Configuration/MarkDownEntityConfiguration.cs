namespace Markdown.Infrastructure.Configuration;

public class MarkDownEntityConfiguration : IEntityTypeConfiguration<MarkDown>
{
    public void Configure(EntityTypeBuilder<MarkDown> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.ToTable("MarkDown");

        // 审核状态（P1-3）：默认值=草稿，与实体默认一致——数据库默认"审核通过"会让
        // SQL 直插/漏填 Status 的文章绕过审核直接对外可见；已有行的值不受默认值变更影响
        builder.Property(x => x.Status)
            .HasDefaultValue(MarkStatus.MarkDraft);
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