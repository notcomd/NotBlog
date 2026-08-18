
namespace Video.Infrastructure.DbConfig;

public class VideoReviewDbContextConfiguration : IEntityTypeConfiguration<VideoReview>
{
    public void Configure(EntityTypeBuilder<VideoReview> builder)
    {
        builder.ToTable("VideoReview");
        builder.HasKey(en => en.VideoReviewGuid);
        builder.HasIndex(en => en.VideoReviewGuid);
        builder.HasIndex(en => en.VideoGuid);
        builder.HasIndex(en => en.UserGuid);
        builder.HasIndex(en => en.RootReview);
        builder.Ignore(en => en.DomainEvents);
        builder.Ignore(en => en.Id);
        // builder.Ignore(en => en.VideoImages);       // delegated via Content.MediaItems
        // builder.Ignore(en => en.VideoReviewBody);   // delegated via Content.Body
       // builder.Property(en => en.Id).UseHiLo("Reviewq");
        builder.Property(en => en.VideoGuid).IsRequired();
        builder.OwnsOne(en => en.VideoControl, x =>
        {
            x.ToJson();
            x.Property(s => s.AuthorVideo).HasJsonPropertyName("AuthorVideo");
            x.Property(s => s.VideoDelete).HasJsonPropertyName("VideoDelete");
            x.Property(s => s.VideoDisplay).HasJsonPropertyName("VideoDisplay");
            x.OwnsOne(en => en.VideoProtectedTime, x =>
            {                
                x.Property(s => s.StartTime).HasJsonPropertyName("StartTime");
                x.Property(s => s.EndTime).HasJsonPropertyName("EndTime");
            });
            x.OwnsOne(en => en.TimeSpace, x =>
            {              
                x.Property(s => s.CreateAt).HasJsonPropertyName("VideoControlCreateTime");
                x.Property(s => s.UpdateAt).HasJsonPropertyName("VideoControlUpdateTime");
            });
        });
        builder.OwnsOne(en => en.VideoQuote, x =>
        {
            x.ToJson();
            x.Property(s => s.Upvote).HasJsonPropertyName("c_Upvote");
            x.Property(s => s.Stars).HasJsonPropertyName("c_Stars");
            x.Property(s => s.Watch).HasJsonPropertyName("c_Watch");
            x.Property(s => s.Down).HasJsonPropertyName("c_Down");
            x.Property(s => s.Ballot).HasJsonPropertyName("c_Ballot");
            x.Property(s => s.Share).HasJsonPropertyName("c_Share");
        });
        builder.OwnsOne(en => en.TimeSpace, x =>
        {
            x.ToJson();
            x.Property(s => s.UpdateAt).HasJsonPropertyName("UpdateTime");
            x.Property(s => s.CreateAt).HasJsonPropertyName("CreateTime");
        });
        
        builder.HasMany(e => e.VideoReviews)
            .WithOne()
            .HasForeignKey(e => e.VideoGuid)
            .OnDelete(DeleteBehavior.NoAction);

        // ── ReviewContent (new multi-type content model) ──
        builder.OwnsOne(e => e.Content, x =>
        {
            x.ToJson();
            x.Property(s => s.ContentType)
                .HasJsonPropertyName("ContentType")
                .HasMaxLength(20)
                .IsRequired();

            x.Property(s => s.Body)
                .HasJsonPropertyName("ReviewBody")
                .HasMaxLength(10000);

            // OwnsMany for MediaItems (replaces direct VideoImages mapping)
            x.OwnsMany(s => s.MediaItems, mi =>
            {             
                mi.Property(v => v.Description);
                mi.Property(v => v.ImageUrl);
                mi.Property(v => v.SortOrder);
            });
     
        });

        builder.OwnsMany(e=>e.VideoImages,x=>
        {
            x.ToJson();
            x.Property(v => v.ImageUrl);
            x.Property(v => v.ThumbnailUrl);
            x.Property(v => v.Width);
            x.Property(v => v.Height);
            x.Property(v => v.Format).HasMaxLength(16);
            x.Property(v => v.FileSize);
            x.Property(v => v.Description);
            x.Property(v => v.SortOrder);
        });
    }
}