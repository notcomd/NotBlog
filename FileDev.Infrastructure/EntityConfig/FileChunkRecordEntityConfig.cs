using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class FileChunkRecordEntityConfig : IEntityTypeConfiguration<FileChunkRecord>
{
    public void Configure(EntityTypeBuilder<FileChunkRecord> builder)
    {
        builder.Ignore(e => e.DomainEventbus);

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
                    .Select(int.Parse).ToHashSet()
            )
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
                    : new HashSet<string>()
            )
            .HasMaxLength(2000);

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
