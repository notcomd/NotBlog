namespace Message.Infrastructure.EntityConfig;

/// <summary>配置评论实体 <c>Comment</c> 到 Comments 表的映射。</summary>
public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.CommentGuid);

        builder.Property(c => c.CommentGuid)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(c => c.TweetGuid)
            .IsRequired();

        builder.Property(c => c.UserGuid)
            .IsRequired();

        builder.Property(c => c.ParentGuid);

        builder.Property(c => c.ReplyToGuid);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.LikeCount);

        builder.Property(c => c.ReplyCount);

        builder.Property(c => c.IsDeleted)
            .IsRequired();

        builder.Property(c => c.CreateTime)
            .IsRequired();

        builder.Ignore("_domainEvents");

        builder.HasIndex(c => new { c.TweetGuid, c.CreateTime });
        builder.HasIndex(c => c.ParentGuid);
        builder.HasIndex(c => c.UserGuid);
    }
}
