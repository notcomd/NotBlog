using FileDev.Domain.DomainEntities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructres.EntityConfigurtion
{
    public class FileGroupEntityConfiguration : IEntityTypeConfiguration<FileGroup>
    {
        public void Configure(EntityTypeBuilder<FileGroup> builder)
        {

            builder.HasKey(en => en.Id);

            builder.Ignore(en => en.DomainEvents);


            builder.HasOne(fg => fg.Parent)
                .WithMany(fg => fg.ChildFileGroups)
                .HasForeignKey(fg => fg.ParentId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.HasMany(fg => fg.Files)
                .WithOne()
                .HasForeignKey(fg => fg.FileGroupGuid);

           

        }
    }
}
