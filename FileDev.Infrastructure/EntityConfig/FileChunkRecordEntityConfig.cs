using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class FileChunkRecordEntityConfig : IEntityTypeConfiguration<FileChunkRecord>
{
    public void Configure(EntityTypeBuilder<FileChunkRecord> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.ToTable("FileChunkRecord");

        builder.HasKey(e => e.RecordId);

        builder.Property(e => e.FileKey)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(e => e.FileKey)
            .IsUnique()
            .HasDatabaseName("IX_FileChunkRecord_FileKey");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_FileChunkRecord_UserId");

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.TotalSize).IsRequired();
        builder.Property(e => e.ChunkSize).IsRequired();
        builder.Property(e => e.TotalChunks).IsRequired();

        builder.Property(e => e.UploadedChunks)
            .HasConversion(
                v => string.Join(",", v.OrderBy(x => x)),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse).ToList(),
                new ValueComparer<List<int>>(
                    (l, r) => l!.SequenceEqual(r!),
                    v => v.Aggregate(0, (a, x) => HashCode.Combine(a, x.GetHashCode())),
                    v => new List<int>(v)))
            .HasColumnName("UploadedChunksCsv")
            .HasMaxLength(4000);

        builder.Property(e => e.FileMd5)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.FileType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.FileIdentity)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.FileTags)
            .HasConversion(
                v => v != null ? string.Join(",", v) : string.Empty,
                v => !string.IsNullOrEmpty(v)
                    ? v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet()
                    : new HashSet<string>(),
                new ValueComparer<HashSet<string>>(
                    (l, r) => l!.SetEquals(r!),
                    v => v.Aggregate(0, (a, s) => HashCode.Combine(a, s.GetHashCode())),
                    v => new HashSet<string>(v)))
            .HasMaxLength(2000)
            // 历史数据库结构（20260730135155_BlogFileStrong）中该列为可空，显式保持可空以对齐既有结构
            .IsRequired(false);

        builder.Property(e => e.FileDescription).HasMaxLength(500);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("IX_FileChunkRecord_Status_CreatedAt");

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CompletedAt);
    }
}
