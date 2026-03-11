using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Video.Domain.Entities;

namespace Video.Infrastructure.DbConfig;

public class VideoDbContextConfiguration : IEntityTypeConfiguration<Videos>
{
    public void Configure(EntityTypeBuilder<Videos> builder)
    {
        builder.HasKey(en => en.VideoGuid);

        builder.HasIndex(en => en.VideoGuid);

        builder.Property(en => en.VideoTags).HasColumnType("text[]").HasColumnName("VideoTags");

        builder.OwnsOne(en => en.VideoQuote, x =>
        {
            x.ToJson();
            x.Property(s => s.Upvote).HasColumnName("v_Upvote");
            x.Property(s => s.Stars).HasColumnName("v_Stars");
            x.Property(s => s.Watch).HasColumnName("v_Watch");
            x.Property(s => s.Down).HasColumnName("v_Down");
            x.Property(s => s.Ballot).HasColumnName("v_Ballot");
            x.Property(s => s.Share).HasColumnName("v_Share");
        });

        builder.OwnsMany(en => en.Affiliated, x =>
        {
            x.Property(s => s.AffiliatedUserUuid).HasColumnName("AffiliatedUserUuid").HasColumnType("string");
            x.Property(s => s.AffiliatedAuthorize).HasColumnName("AffiliatedAuthorize");
        });

        builder.OwnsOne(en => en.VideoControl, x =>
        {
            x.ToJson();
            x.Property(s => s.AuthorVideo).HasColumnName("AuthorVideo");
            x.Property(s => s.VideoDelete).HasColumnName("VideoDelete");
            x.Property(s => s.VideoDisplay).HasColumnName("VideoDisplay");
            x.OwnsOne(en => en.VideoProtectedTime, xe =>
            {
                xe.ToJson();
                xe.Property(s => s.StartTime).HasColumnName("StartTime");
                xe.Property(s => s.EndTime).HasColumnName("EndTime");
            });
        });

        builder.HasMany(en => en.VideoReviews)
            .WithOne()
            .HasForeignKey(en => en.VideoGuid)
            .OnDelete(DeleteBehavior.NoAction);

        builder.OwnsOne(en => en.TimeSpace, x =>
        {
            x.ToJson();
            x.Property(s => s.UpdateAt).HasColumnName("UpdateTime");
            x.Property(s => s.CreateAt).HasColumnName("CreateTime");
        });
    }
}