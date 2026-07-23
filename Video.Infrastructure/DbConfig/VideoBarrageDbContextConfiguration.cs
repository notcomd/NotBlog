using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Video.Domain.Entities;

namespace Video.Infrastructure.DbConfig;

public class VideoBarrageDbContextConfiguration : IEntityTypeConfiguration<VideoBarrage>
{
    public void Configure(EntityTypeBuilder<VideoBarrage> builder)
    {
        builder.ToTable("VideoBarrage");
        builder.HasKey(x => x.VideoBarrageGuid);

        builder.HasIndex(x => x.VideoGuid);
        builder.HasIndex(x => x.UserGuid);

        builder.Property(x => x.VideoBarrageBody).IsRequired();

        builder.OwnsOne(x => x.TimeSpace, b =>
        {
            b.ToJson();
            b.Property(p => p.CreateAt).HasColumnName("CreateTime");
            b.Property(p => p.UpdateAt).HasColumnName("UpdateTime");
        });

        builder.OwnsOne(x => x.VideoControl, b =>
        {
            b.ToJson();
            b.Property(p => p.AuthorVideo).HasColumnName("AuthorVideo");
            b.Property(p => p.VideoDelete).HasColumnName("VideoDelete");
            b.Property(p => p.VideoDisplay).HasColumnName("VideoDisplay");
        });

        builder.OwnsOne(x => x.VideoImage, b =>
        {
            b.ToJson();
            b.Property(p => p.ImageUrl);
            b.Property(p => p.Description);
            b.Property(p => p.SortOrder);
        });

        builder.OwnsOne(x => x.VideoQuote, b =>
        {
            b.ToJson();
            b.Property(p => p.Upvote).HasColumnName("b_Upvote");
            b.Property(p => p.Stars).HasColumnName("b_Stars");
            b.Property(p => p.Watch).HasColumnName("b_Watch");
            b.Property(p => p.Down).HasColumnName("b_Down");
            b.Property(p => p.Ballot).HasColumnName("b_Ballot");
            b.Property(p => p.Share).HasColumnName("b_Share");
        });
    }
}