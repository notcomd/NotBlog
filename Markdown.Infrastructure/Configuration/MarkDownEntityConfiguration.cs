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
        builder.Property(x => x.Id).UseHiLo("MarkDownGuid");
        builder.HasKey(x => x.Id);

        // 文件元数据列（正文文件化：内容存文件存储后端，DB 只存引用与统计）
        builder.Property(x => x.FileId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FileUri).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.FileSize).IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.FileExt).HasMaxLength(32).IsRequired();
        builder.Property(x => x.MarkDownHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CoverUrl).HasMaxLength(2048);

        // 文档交互统计 MarkQuote（值对象，6 列）
        builder.OwnsOne(x => x.MarkQuote, quoteBuilder =>
        {
            quoteBuilder.Property(q => q.LoveSome).HasColumnName("LoveCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.FavoriteSome).HasColumnName("FavoriteCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.ShareSome).HasColumnName("ShareCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.CoinSome).HasColumnName("CoinCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.ViewSome).HasColumnName("ViewCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.HeatScore).HasColumnName("HeatScore").HasDefaultValue(0.0);
        });

        builder.HasMany(en => en.MarkReviews)
            .WithOne(en => en.MarkDown)
            .HasForeignKey(en => en.MarkDownGuid)
            .HasPrincipalKey(en => en.MarkDownGuid);

        builder.HasMany(en => en.OldMarkDowns)
            .WithOne(en => en.MarkDown)
            .HasForeignKey(en => en.MarkDownGuid)
            .HasPrincipalKey(en => en.MarkDownGuid);

        // 标签集合映射为 JSON 列（List<string>，与实体类型一致）
        builder.Property(x => x.MarkDownTagboard)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("text");
    }
}
