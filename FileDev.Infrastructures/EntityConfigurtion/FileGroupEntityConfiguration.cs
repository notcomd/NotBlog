using FileDev.Domain.Entities;

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

            builder.HasMany(en => en.ChlidrenFileGroup).WithMany();

        }
    }
}
