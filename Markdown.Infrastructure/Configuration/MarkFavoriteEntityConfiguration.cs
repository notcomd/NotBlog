namespace Markdown.Infrastructure.Configuration;

public class MarkFavoriteEntityConfiguration : IEntityTypeConfiguration<MarkFavorite>
{
    public void Configure(EntityTypeBuilder<MarkFavorite> builder)
    {
        builder.Ignore(en => en.DomainEvents);

        // Tags 为内存集合（充血模型入口），持久化由 TagsJson 文本列承担
        builder.Ignore(x => x.Tags);

        builder.ToTable("MarkFavorite");
        builder.Property(x => x.Id).UseHiLo("MarkFavoriteGuid");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MarkFavoriteGuid).IsRequired();
        builder.Property(x => x.UserGuid).IsRequired();
        builder.Property(x => x.MarkDownGuid).IsRequired();

        // 标签 JSON 数组文本列（供 SQL LIKE 精确过滤，见 MarkFavoriteListQueryHandler）
        builder.Property(x => x.TagsJson)
            .HasColumnType("text")
            .IsRequired();

        // 同一用户对同一文章仅能收藏一次（并发安全兜底）
        builder.HasIndex(x => new { x.UserGuid, x.MarkDownGuid }).IsUnique();

        // 我的收藏按用户分页查询 / 收藏列表联表文章
        builder.HasIndex(x => x.UserGuid);
        builder.HasIndex(x => x.MarkDownGuid);
    }
}
