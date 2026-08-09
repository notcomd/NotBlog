
namespace Message.Infrastructure.EntityConfig;

public class TweetConfiguration : IEntityTypeConfiguration<Tweet>
{
    public void Configure(EntityTypeBuilder<Tweet> builder)
    {
        builder.ToTable("Tweets");

        builder.HasKey(t => t.TweetGuid);

        builder.Property(t => t.TweetGuid)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(t => t.AuthorGuid)
            .IsRequired();

        builder.Property(t => t.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(t => t.Media)
            .HasColumnName("MediaUrls")
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<TweetMedia>>(v, (JsonSerializerOptions?)null) ?? new List<TweetMedia>())
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.LinkMetadata)
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LinkMetadata>(v, (JsonSerializerOptions?)null));

        builder.Property(t => t.Hashtags)
            .HasColumnName("Hashtags")
            .HasColumnType("text")
            .HasConversion(
                v => string.Join(",", v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet())
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.TweetStatus)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.Visibility)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.IsPinned)
            .IsRequired();

        builder.Property(t => t.ViewCount);

        builder.Property(t => t.LikeCount);

        builder.Property(t => t.CommentCount);

        builder.Property(t => t.ShareCount);

        builder.Property(t => t.CoinCount);

        builder.Property(t => t.FavoriteCount);

        builder.Property(t => t.HotScore);

        builder.Property(t => t.CircleGuid);

        builder.Property(t => t.TopicGuidsJson)
            .HasColumnName("TopicGuids")
            .HasColumnType("text");

        builder.Property(t => t.AuditReason)
            .HasMaxLength(500);

        builder.Property(t => t.PublishTime);

        builder.Property(t => t.CreateTime)
            .IsRequired();

        builder.Property(t => t.UpdateTime)
            .IsRequired();

        builder.HasIndex(t => t.AuthorGuid);
        builder.HasIndex(t => new { t.TweetStatus, t.CreateTime }).IsDescending(false, true);
        builder.HasIndex(t => t.HotScore).IsDescending();
        builder.HasIndex(t=>t.Hashtags).IsDescending();
        builder.HasIndex(t => t.CreateTime).IsDescending();
        builder.HasIndex(t => t.CircleGuid);
    }
}
