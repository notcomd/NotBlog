using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class NotFileGroupEntityConfiguration : IEntityTypeConfiguration<NotFileGroup>
{
    public void Configure(EntityTypeBuilder<NotFileGroup> builder)
    {
        builder.Ignore(en => en.DomainEvents);
        builder.ToTable("NotFileGroup");
        //builder.Property(x => x.Id).UseHiLo("NotFileGroupseq");
        builder.Ignore(x=>x.Id);
        builder.HasKey(x => x.NotFileGroupId);

        
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentGroupId)
            .HasPrincipalKey(x => x.NotFileGroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_NotFileGroup_UserId");

        
        builder.HasIndex(x => new { x.UserId, x.ParentGroupId, x.FileGroupName })
            .HasDatabaseName("IX_NotFileGroup_UserId_ParentGroupId_FileGroupName");

        builder.Property(x => x.FileGroupName).HasMaxLength(256);

        
        builder.Property(x => x.FileIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<Guid>(),
                new ValueComparer<HashSet<Guid>>(
                    (l, r) => l!.SetEquals(r!),
                    v => v.Aggregate(0, (a, g) => HashCode.Combine(a, g.GetHashCode())),
                    v => new HashSet<Guid>(v)))
            .HasColumnType("text");

        builder.Property(x => x.FileGroupTags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<string>(),
                new ValueComparer<HashSet<string>>(
                    (l, r) => l!.SetEquals(r!),
                    v => v.Aggregate(0, (a, s) => HashCode.Combine(a, s.GetHashCode())),
                    v => new HashSet<string>(v)))
            .HasColumnType("text");
    }
}
