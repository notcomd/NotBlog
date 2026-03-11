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
        builder.HasKey(en => en.VideoReviewGuid);
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
    }
}