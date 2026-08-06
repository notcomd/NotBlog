namespace Markdown.Infrastructure.Configuration;

public class MarkFavoriteTagEntityConfiguration : IEntityTypeConfiguration<MarkFavoriteTag>
{
    public void Configure(EntityTypeBuilder<MarkFavoriteTag> builder)
    {
        builder.Ignore(en => en.DomainEvents);

        builder.ToTable("MarkFavoriteTag");
        builder.Property(x => x.Id).UseHiLo("MarkFavoriteTagGuid");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MarkFavoriteTagGuid).IsRequired();
        builder.Property(x => x.UserGuid).IsRequired();
        builder.Property(x => x.Tag).HasMaxLength(MarkFavoriteTag.MaxTagLength).IsRequired();
        builder.Property(x => x.UseCount).HasDefaultValue(0);
        builder.Property(x => x.LastUsedAt).IsRequired();
        builder.Property(x => x.CreateAt).IsRequired();

        // 同一用户同一标签仅一条记录（标签库 upsert 并发安全）
        builder.HasIndex(x => new { x.UserGuid, x.Tag }).IsUnique();

        // 按用户查询标签建议
        builder.HasIndex(x => x.UserGuid);
    }
}
