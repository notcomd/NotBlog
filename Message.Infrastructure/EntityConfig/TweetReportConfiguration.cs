namespace Message.Infrastructure.EntityConfig;

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

        builder.Property("_evidenceUrls")
            .HasColumnName("EvidenceUrls")
            .HasColumnType("nvarchar(max)");

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
