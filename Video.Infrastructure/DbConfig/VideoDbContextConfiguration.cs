
namespace Video.Infrastructure.DbConfig;

public class VideoDbContextConfiguration : IEntityTypeConfiguration<Videos>
{
    public void Configure(EntityTypeBuilder<Videos> builder)
    {
        builder.ToTable("Video");
        builder.Ignore(e => e.DomainEvents);

        builder.Property(en => en.Id).UseHiLo("VideoGuid");
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
            b.Property(p => p.Upvote).HasColumnName("v_Upvote");
            b.Property(p => p.Stars).HasColumnName("v_Stars");
            b.Property(p => p.Watch).HasColumnName("v_Watch");
            b.Property(p => p.Down).HasColumnName("v_Down");
            b.Property(p => p.Ballot).HasColumnName("v_Ballot");
            b.Property(p => p.Share).HasColumnName("v_Share");
        });

        builder.OwnsOne(e => e.VideoControl, b =>
        {
            b.ToJson();
            b.Property(p => p.AuthorVideo).HasColumnName("AuthorVideo");
            b.Property(p => p.VideoDelete).HasColumnName("VideoDelete");
            b.Property(p => p.VideoDisplay).HasColumnName("VideoDisplay");
            b.OwnsOne(p => p.VideoProtectedTime, pb =>
            {
                pb.ToJson();
                pb.Property(p => p.StartTime).HasColumnName("StartTime");
                pb.Property(p => p.EndTime).HasColumnName("EndTime");
            });
        });

        builder.HasMany(e => e.VideoReviews)
            .WithOne()
            .HasForeignKey(e => e.VideoGuid)
            .OnDelete(DeleteBehavior.NoAction);

        builder.OwnsOne(e => e.TimeSpace, b =>
        {
            b.ToJson();
            b.Property(p => p.UpdateAt).HasColumnName("UpdateTime");
            b.Property(p => p.CreateAt).HasColumnName("CreateTime");
        });
    }
}