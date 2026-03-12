using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markdown.Infrastructure.Configuration;

public class MarkReviewEntityConfiguration:IEntityTypeConfiguration<MarkReview>
{
    public void Configure(EntityTypeBuilder<MarkReview> builder)
    {
        builder.Ignore(en => en.DomainEventbus);
        builder.Property(en => en.Id).UseHiLo("MarkReviewq");

        builder.HasMany(en => en.MarkReviews)
            .WithOne().HasForeignKey(en=>en.MarkAggregateRootGuid);

        builder.HasIndex(en => en.UserId);

        builder.OwnsOne(en => en.MarkQuote);
        
        
    }
}