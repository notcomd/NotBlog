
namespace Video.Infrastructure.DbConfig;

public class VideoCollectionDbContextConfiguration : IEntityTypeConfiguration<VideoCollection>
{
    public void Configure(EntityTypeBuilder<VideoCollection> builder)
    {
        builder.ToTable("VideoCollection");
        builder.HasKey(en => en.VideoCollectionGuid);

        builder.HasIndex(en => en.VideoCollectionGuid);


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

        builder.OwnsOne(en => en.VideoControl, x =>
        {
            x.ToJson();
            x.Property(s => s.AuthorVideo).HasJsonPropertyName("AuthorVideo");
            x.Property(s => s.VideoDelete).HasJsonPropertyName("VideoDelete");
            x.Property(s => s.VideoDisplay).HasJsonPropertyName("VideoDisplay");
            x.OwnsOne(en => en.VideoProtectedTime, x =>
            {
                x.Property(s => s.StartTime).HasJsonPropertyName("VideoProtectedStartTime");
                x.Property(s => s.EndTime).HasJsonPropertyName("VideoProtectedEndTime");
            });
            x.OwnsOne(en=>en.TimeSpace, x =>
            {
                x.Property(s => s.CreateAt).HasJsonPropertyName("VideoControlCreateTime");
                x.Property(s => s.UpdateAt).HasJsonPropertyName("VideoControlUpdateTime");
            });
        });

        builder.OwnsOne(en => en.TimeSpace, x =>
        {
            x.ToJson();
            x.Property(s => s.UpdateAt).HasJsonPropertyName("UpdateTime");
            x.Property(s => s.CreateAt).HasJsonPropertyName("CreateTime");
        });
    }
}