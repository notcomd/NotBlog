

using Markdown.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markdown.Infrastructres.EntityConfig;


public class MarkDownEntityTypeConfiguretion : IEntityTypeConfiguration<MarkDown>
{
    public void Configure(EntityTypeBuilder<MarkDown> builder)
    {

        builder.ToTable("MarkDowns");
        builder.Ignore(x => x.DomainEvents);
        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.MarkDownBlocks)
            .WithOne()
            .HasForeignKey(x => x.MarkDownGuid)
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