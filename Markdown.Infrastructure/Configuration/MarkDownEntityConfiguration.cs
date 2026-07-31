namespace Markdown.Infrastructure.Configuration;

public class MarkDownEntityConfiguration : IEntityTypeConfiguration<MarkDown>
{
    public void Configure(EntityTypeBuilder<MarkDown> builder)
    {
        builder.Ignore(en => en.DomainEventbus);
        builder.ToTable("NotFileGroup");
        builder.Property(x => x.Id).UseHiLo("NotFileGroupGuid");
        builder.HasKey(x => x.Id);

        builder.HasMany(en => en.MarkReviews)
            .WithOne(en => en.MarkDown)
            .HasForeignKey(en => en.MarkDownGuid);

        builder.HasMany(en => en.OldMarkDowns)
            .WithOne(en => en.MarkDown)
            .HasForeignKey(en => en.MarkDownGuid);

        // HashSet 集合映射为 JSON 列
        builder.Property(x => x.MarkDownTagboard)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<string>())
            .HasColumnType("text");
    }
}