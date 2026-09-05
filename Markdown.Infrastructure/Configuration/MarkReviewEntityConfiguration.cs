namespace Markdown.Infrastructure.Configuration;

public class MarkReviewEntityConfiguration : IEntityTypeConfiguration<MarkReview>
{
    public void Configure(EntityTypeBuilder<MarkReview> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.Ignore(x=>x.Id);

        builder.ToTable("MarkReview");
        builder.HasKey(x => x.MarkDownGuid);

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

        // 配置 ReviewQuote 为 owned entity（值对象，4 列：点赞/查看/回复数/踩）
        builder.OwnsOne(x => x.ReviewQuote, quoteBuilder =>
        {
            quoteBuilder.Property(q => q.LoveSome).HasColumnName("LoveCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.ViewSome).HasColumnName("ViewCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.ReplySome).HasColumnName("ReplyCount").HasDefaultValue(0);
            quoteBuilder.Property(q => q.DislikeSome).HasColumnName("DislikeCount").HasDefaultValue(0);
        });

        // 评论配图作为 owned 值对象集合整体序列化为 JSONB 列（无独立表、无外键）。
        // 图片为已上传资源的引用列表，无独立身份/生命周期，用 ToJson() 内联存储最贴切。
        builder.OwnsMany(x => x.ReviewImages, reviewImageBuilder =>
        {
            reviewImageBuilder.ToJson();
        });

        // 配置索引（提高查询性能）
        builder.HasIndex(x => x.MarkDownGuid);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.MarkAggregateRootGuid);
    }
}
