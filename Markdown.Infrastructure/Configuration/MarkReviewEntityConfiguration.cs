namespace Markdown.Infrastructure.Configuration;

public class MarkReviewEntityConfiguration : IEntityTypeConfiguration<MarkReview>
{
    public void Configure(EntityTypeBuilder<MarkReview> builder)
    {
        builder.Ignore(en => en.DomainEventbus);
        builder.ToTable("MarkReview");
        builder.Property(x => x.Id).UseHiLo("MarkReviewGuid");
        builder.HasKey(x => x.Id);

        // 配置 MarkReviewGuid 为必填字段
        builder.Property(x => x.MarkReviewGuid).IsRequired();

        // 配置外键关系：MarkReview -> MarkDown（通过聚合根访问）
        builder.HasOne(x => x.MarkDown)
            .WithMany(x => x.MarkReviews)
            .HasForeignKey(x => x.MarkDownGuid)
            .HasPrincipalKey(x => x.MarkDownGuid)
            .OnDelete(DeleteBehavior.Cascade); // 级联删除：MarkDown 删除时自动删除评论

        // 配置自引用关系：父评论 -> 子评论
        builder.HasOne<MarkReview>()
            .WithMany(x => x.MarkReviews)
            .HasForeignKey(x => x.MarkAggregateRootGuid)
            .HasPrincipalKey(x => x.MarkReviewGuid)
            .OnDelete(DeleteBehavior.Restrict); // 限制删除，手动管理级联

        // 配置 MarkQuote 为 owned entity（值对象）
        builder.OwnsOne(x => x.MarkQuote, quoteBuilder =>
        {
            quoteBuilder.Property(q => q.LoveSome).HasColumnName("LoveCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.ReviewSome).HasColumnName("ReplyCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.CommentSome).HasColumnName("CommentCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.ShareSome).HasColumnName("ShareCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.ViewSome).HasColumnName("ViewCount").HasDefaultValue(0);
        });

        // 配置索引（提高查询性能）
        builder.HasIndex(x => x.MarkDownGuid);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.MarkAggregateRootGuid);
    }
}