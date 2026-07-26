using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Video.Domain.Entities;

namespace Video.Infrastructure.DbConfig;

public class VideoReviewDbContextConfiguration : IEntityTypeConfiguration<VideoReview>
{
    public void Configure(EntityTypeBuilder<VideoReview> builder)
    {
        builder.ToTable("VideoReview");
        builder.HasIndex(en => en.VideoReviewGuid);
        builder.HasIndex(en => en.VideoGuid);
        builder.HasIndex(en => en.UserGuid);
        builder.HasIndex(en => en.RootReview);
        builder.Ignore(en => en.DomainEventbus);
        builder.Ignore(en => en.VideoImages);       // delegated via Content.MediaItems
        builder.Ignore(en => en.VideoReviewBody);   // delegated via Content.Body
        builder.Property(en => en.Id).UseHiLo("Reviewq");
        builder.Property(en => en.VideoGuid).IsRequired();
        builder.OwnsOne(en => en.VideoControl, x =>
        {
            x.ToJson();
            x.Property(s => s.AuthorVideo).HasColumnName("AuthorVideo");
            x.Property(s => s.VideoDelete).HasColumnName("VideoDelete");
            x.Property(s => s.VideoDisplay).HasColumnName("VideoDisplay");
            //x.Property(s => s.VideoProtectedTime).HasColumnName("VideoProtectedTime");
            x.OwnsOne(en => en.VideoProtectedTime, x =>
            {
                x.ToJson();
                x.Property(s => s.StartTime).HasColumnName("StartTime");
                x.Property(s => s.EndTime).HasColumnName("EndTime");
                //x.WithOwner().HasForeignKey("VideoReviewGuid");
            });
            //x.WithOwner().HasForeignKey("VideoReviewGuid");
        });
        builder.OwnsOne(en => en.VideoQuote, x =>
        {
            x.ToJson();
            x.Property(s => s.Upvote).HasColumnName("c_Upvote");
            x.Property(s => s.Stars).HasColumnName("c_Stars");
            x.Property(s => s.Watch).HasColumnName("c_Watch");
            x.Property(s => s.Down).HasColumnName("c_Down");
            x.Property(s => s.Ballot).HasColumnName("c_Ballot");
            x.Property(s => s.Share).HasColumnName("c_Share");
            //x.WithOwner().HasForeignKey("VideoReviewGuid");
        });
        builder.OwnsOne(en => en.TimeSpace, x =>
        {
            x.ToJson();
            x.Property(s => s.UpdateAt).HasColumnName("UpdateTime");
            x.Property(s => s.CreateAt).HasColumnName("CreateTime");
        });
        
        builder.HasMany(e => e.VideoReviews)
            .WithOne()
            .HasForeignKey(e => e.VideoGuid)
            .OnDelete(DeleteBehavior.NoAction);

        // ── ReviewContent (new multi-type content model) ──
        builder.OwnsOne(e => e.Content, x =>
        {
            x.Property(s => s.ContentType)
                .HasColumnName("ContentType")
                .HasMaxLength(20)
                .IsRequired();

            x.Property(s => s.Body)
                .HasColumnName("ReviewBody")
                .HasMaxLength(10000);

            // OwnsMany for MediaItems (replaces direct VideoImages mapping)
            x.OwnsMany(s => s.MediaItems, mi =>
            {
                mi.ToJson();
                mi.Property(v => v.Description);
                mi.Property(v => v.ImageUrl);
                mi.Property(v => v.SortOrder);
            });
        });
    }
}