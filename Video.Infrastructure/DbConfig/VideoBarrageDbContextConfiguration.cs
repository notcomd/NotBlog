
namespace Video.Infrastructure.DbConfig;

public class VideoBarrageDbContextConfiguration : IEntityTypeConfiguration<VideoBarrage>
{
    public void Configure(EntityTypeBuilder<VideoBarrage> builder)
    {
        builder.ToTable("VideoBarrage");
        builder.HasKey(x => x.VideoBarrageGuid);

        builder.HasIndex(x => x.VideoGuid);
        builder.HasIndex(x => x.UserGuid);

        builder.Property(x => x.VideoBarrageBody).IsRequired(false);

        builder.Property(x => x.BarrageType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired()
            .HasDefaultValue(BarrageType.Text);

        builder.OwnsOne(x => x.TimeSpace, b =>
        {
            b.ToJson();
            b.Property(p => p.CreateAt).HasJsonPropertyName("CreateTime");
            b.Property(p => p.UpdateAt).HasJsonPropertyName("UpdateTime");
        });

     
        builder.OwnsOne(x => x.VideoControl, b =>
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

        builder.OwnsMany(x => x.VideoImages, b =>
        {
            b.ToJson();
            b.Property(p => p.ImageUrl);
            b.Property(p => p.ThumbnailUrl);
            b.Property(p => p.Width);
            b.Property(p => p.Height);
            b.Property(p => p.Format).HasMaxLength(16);
            b.Property(p => p.FileSize);
            b.Property(p => p.Description);
            b.Property(p => p.SortOrder);
        });

        builder.OwnsOne(x => x.VideoQuote, b =>
        {
            b.ToJson();
            b.Property(p => p.Upvote).HasJsonPropertyName("b_Upvote");
            b.Property(p => p.Stars).HasJsonPropertyName("b_Stars");
            b.Property(p => p.Watch).HasJsonPropertyName("b_Watch");
            b.Property(p => p.Down).HasJsonPropertyName("b_Down");
            b.Property(p => p.Ballot).HasJsonPropertyName("b_Ballot");
            b.Property(p => p.Share).HasJsonPropertyName("b_Share");
        });
    }
}
