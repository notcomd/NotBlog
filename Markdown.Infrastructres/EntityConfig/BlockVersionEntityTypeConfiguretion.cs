


using Markdown.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markdown.Infrastructres.EntityConfig;

public class BlockVersionEntityTypeConfiguretion : IEntityTypeConfiguration<BlockVersion>
{
    public void Configure(EntityTypeBuilder<BlockVersion> builder)
    {
        builder.ToTable("BlockVersions");
        builder.HasKey(bv => bv.Id);
        builder.Ignore(bv => bv.DomainEvents);


        builder.Property(bv => bv.Id)
            .ValueGeneratedOnAdd();
        builder.Property(bv => bv.UserId)
            .IsRequired();
        builder.Property(bv => bv.MarkDownGuid)
            .IsRequired();
        builder.Property(bv => bv.BlockIndex)
            .IsRequired();
        builder.Property(bv => bv.BlockType)
            .IsRequired();

        builder.Property(bv => bv.SaveTime)
            .IsRequired();
        builder.Property(bv => bv.MarkDownContent)
            .HasColumnType("TEXT");

        builder.Property(bv => bv.ChangeMessage)
            .HasMaxLength(500);
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