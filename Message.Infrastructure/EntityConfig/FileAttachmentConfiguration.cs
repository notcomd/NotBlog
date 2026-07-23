namespace Message.Infrastructure.EntityConfig;

public class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> builder)
    {
        builder.ToTable("FileAttachments");

        builder.HasKey(fa => fa.AttachmentId);

        builder.Property(fa => fa.AttachmentId)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(fa => fa.MessageId)
            .IsRequired();

        builder.Property(fa => fa.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(fa => fa.FileType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(fa => fa.FileSize)
            .IsRequired();

        builder.Property(fa => fa.FileUri)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(fa => fa.ThumbnailUri)
            .HasMaxLength(2048);

        builder.Property(fa => fa.MimeType)
            .HasMaxLength(100);

        builder.Property(fa => fa.Description)
            .HasMaxLength(500);

        builder.Property(fa => fa.UploadTime)
            .IsRequired();

        builder.Property(fa => fa.IsDeleted)
            .IsRequired();

        builder.HasIndex(fa => fa.MessageId);
    }
}