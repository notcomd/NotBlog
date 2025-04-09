using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Video.Domain.Entities;

namespace Video.Infrastructure.DbConfig;

public class VideoBarrageDbContextConfiguration : IEntityTypeConfiguration<VideoBarrage>
{
    public void Configure(EntityTypeBuilder<VideoBarrage> builder)
    {
        builder.HasKey(x => x.VideoBarrageGuid);
    }
}