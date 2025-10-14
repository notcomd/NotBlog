

using Markdown.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Markdown.Infrastructres.EntityConfig;


public class MarkReviewEntityTypeConfiguretion : IEntityTypeConfiguration<MarkReview>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<MarkReview> builder)
    {
        builder.ToTable("MarkReviews");
        builder.Ignore(x => x.DomainEvents);
        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.MarkChildReviews)
            .WithOne()
            .HasForeignKey(x => x.MarkAggregateRootGuid)
            .OnDelete(DeleteBehavior.Restrict);


        builder.OwnsOne(bv => bv.MarkQuote, mq =>
        {
            mq.Property(m => m.LoveSome)
                .HasDefaultValue(0);
            mq.Property(m => m.QuoteSome)
                .HasDefaultValue(0);
            mq.Property(m => m.QuoteStar)
                .HasDefaultValue(0);
            mq.Property(m => m.CommentSome)
                .HasDefaultValue(0);
            mq.Property(m => m.ReviewSome)
                .HasDefaultValue(0);
        });


    }
}