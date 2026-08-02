namespace Markdown.Infrastructure.Configuration;

public class ReviewImageEntityConfiguration : IEntityTypeConfiguration<ReviewImage>
{
    public void Configure(EntityTypeBuilder<ReviewImage> builder)
    {
        builder.ToTable("ReviewImage");
        builder.Property(x => x.Id).UseHiLo("ReviewImageGuid");
        builder.HasKey(x => x.Id);

        // 与 MarkReview 的关系：ReviewImage -> MarkReview（评论删除时级联删除图片）
        builder.HasOne(x => x.MarkReview)
            .WithMany(x => x.ReviewImages)
            .HasForeignKey(x => x.MarkReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        // 索引：提高按评论查询图片的性能
        builder.HasIndex(x => x.MarkReviewId);
    }
}