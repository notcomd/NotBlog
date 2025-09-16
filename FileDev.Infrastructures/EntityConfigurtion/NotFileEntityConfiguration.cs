using FileDev.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructres.EntityConfigurtion
{
    public class NotFileEntityConfiguration : IEntityTypeConfiguration<NotFile>
    {
        public void Configure(EntityTypeBuilder<NotFile> builder)
        {

            builder.HasKey(en => en.Id);

            builder.Ignore(en => en.DomainEvents);

        }
    }
}
