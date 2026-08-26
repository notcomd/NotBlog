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

        
        builder.Property(x => x.FileIds).HasConversion(
                v => string.Join(",", v.OrderBy(x => x)),
                v => v.Split(',',StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList(),
                new ValueComparer<List<Guid>>(
                    (l, r) => l!.SequenceEqual(r!),
                    v => v.Aggregate(0, (a, x) => HashCode.Combine(a, x.GetHashCode())),
                    v => new List<Guid>(v)));

        builder.Property(x => x.FileGroupTags).HasConversion(
            v=>string.Join(",",v.OrderBy(x=>x)),
            v=>v.Split(',',StringSplitOptions.RemoveEmptyEntries)
            .ToList()
        );
    }
}
