
namespace Video.Infrastructure.DbConfig;

public class VideoHistoryDbContextConfiguration : IEntityTypeConfiguration<VideoHistory>
{
    public void Configure(EntityTypeBuilder<VideoHistory> builder)
    {
        builder.ToTable("VideoHistory");
        builder.HasKey(en => en.VideoHistoryGuid);
        builder.Ignore(en => en.DomainEvents);
        builder.Ignore(en=>en.Id);
        builder.HasIndex(en => en.VideoHistoryGuid);

        builder.OwnsOne(en => en.TimeSpace, x =>
        {
            x.ToJson();
            x.Property(s => s.UpdateAt).HasJsonPropertyName("UpdateTime");
            x.Property(s => s.CreateAt).HasJsonPropertyName("CreateTime");
        });
    }
}