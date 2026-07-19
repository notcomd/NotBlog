using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class NotFileGroupEntityConfiguration : IEntityTypeConfiguration<NotFileGroup>
{
    public void Configure(EntityTypeBuilder<NotFileGroup> builder)
    {
        builder.Ignore(en => en.DomainEventbus);
        builder.ToTable("NotFileGroup");
        builder.Property(x => x.Id).UseHiLo("NotFileGroupseq");
        builder.HasKey(x => x.Id);

        // 自引用树形关系
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentGroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // HashSet 集合映射为 JSON 列
        builder.Property(x => x.FileIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<Guid>())
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.FileGroupTags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<string>())
            .HasColumnType("nvarchar(max)");
    }
}
