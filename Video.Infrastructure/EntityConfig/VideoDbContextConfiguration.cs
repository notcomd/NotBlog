
namespace Video.Infrastructure.EntityConfig;

/// <summary>视频实体（Videos）的 EF Core 映射配置。</summary>
public class VideoDbContextConfiguration : IEntityTypeConfiguration<Videos>
{
    public void Configure(EntityTypeBuilder<Videos> builder)
    {
        builder.ToTable("Video");
        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.Id);
       // builder.Property(en => en.Id).UseHiLo("VideoGuid");
        builder.HasKey(e => e.VideoGuid);

        builder.HasIndex(en => en.VideoName);
        builder.HasIndex(en => en.VideoNvid);
        builder.HasIndex(en => en.VideoTags);
        
        builder.Property(e => e.VideoTags)
            .HasColumnType("text[]")
            .HasColumnName("VideoTags");

        builder.OwnsOne(e => e.VideoQuote, b =>
        {
            b.ToJson();
            b.Property(p => p.Upvote).HasJsonPropertyName("v_Upvote");
            b.Property(p => p.Stars).HasJsonPropertyName("v_Stars");
            b.Property(p => p.Watch).HasJsonPropertyName("v_Watch");
            b.Property(p => p.Down).HasJsonPropertyName("v_Down");
            b.Property(p => p.Ballot).HasJsonPropertyName("v_Ballot");
            b.Property(p => p.Share).HasJsonPropertyName("v_Share");
        });

        builder.OwnsOne(e => e.VideoControl, b =>
        {
            b.ToJson();
            b.Property(p => p.AuthorVideo).HasJsonPropertyName("AuthorVideo");
            b.Property(p => p.VideoDelete).HasJsonPropertyName("VideoDelete");
            b.Property(p => p.VideoDisplay).HasJsonPropertyName("VideoDisplay");
            b.OwnsOne(p => p.VideoProtectedTime, pb =>
            {
                
                pb.Property(p => p.StartTime).HasJsonPropertyName("VideoProtectedStartTime");
                pb.Property(p => p.EndTime).HasJsonPropertyName("VideoProtectedEndTime");
            });
            b.OwnsOne(p => p.TimeSpace, pb =>
            {
                
                pb.Property(p => p.CreateAt).HasJsonPropertyName("VideoControlCreateTime");
                pb.Property(p => p.UpdateAt).HasJsonPropertyName("VideoControlUpdateTime");
            });
        });

        builder.HasMany(e => e.VideoReviews)
            .WithOne()
            .HasForeignKey(e => e.VideoGuid)
            .OnDelete(DeleteBehavior.NoAction);

        builder.OwnsOne(e => e.TimeSpace, b =>
        {
            b.ToJson();
            b.Property(p => p.UpdateAt).HasJsonPropertyName("UpdateTime");
            b.Property(p => p.CreateAt).HasJsonPropertyName("CreateTime");
        });
    }
}