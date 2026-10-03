namespace Message.Infrastructure.EntityConfig;

/// <summary>配置动态举报实体 <c>TweetReport</c> 到 TweetReports 表的映射。</summary>
public class TweetReportConfiguration : IEntityTypeConfiguration<TweetReport>
{
    public void Configure(EntityTypeBuilder<TweetReport> builder)
    {
        builder.ToTable("TweetReports");

        builder.HasKey(tr => tr.ReportGuid);

        builder.Property(tr => tr.ReportGuid)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(tr => tr.ReporterGuid)
            .IsRequired();

        builder.Property(tr => tr.TargetType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(tr => tr.TargetGuid)
            .IsRequired();

        builder.Property(tr => tr.ReportReason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(tr => tr.Category)
            .IsRequired()
            .HasConversion<string>();

        // P0 修复（R-10 生成脚本时暴露）：List<string> 必须带值转换器，否则 Npgsql 模型校验直接失败
        builder.Property<List<string>>("_evidenceUrls")
            .HasColumnName("EvidenceUrls")
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(tr => tr.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(tr => tr.ReviewerGuid);

        builder.Property(tr => tr.ReviewNote)
            .HasMaxLength(500);

        builder.Property(tr => tr.ReviewTime);

        builder.Property(tr => tr.CreateTime)
            .IsRequired();

        builder.Ignore("_domainEvents");

        builder.HasIndex(tr => new { tr.TargetType, tr.TargetGuid });
        builder.HasIndex(tr => new { tr.Status, tr.CreateTime });
        builder.HasIndex(tr => tr.ReporterGuid);
    }
}
