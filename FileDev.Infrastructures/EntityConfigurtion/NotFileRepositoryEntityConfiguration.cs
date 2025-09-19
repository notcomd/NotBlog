using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileDev.Domain.DomainEntities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructres.EntityConfigurtion
{
    public class NotFileRepositoryEntityConfiguration : IEntityTypeConfiguration<NotFileRepository>
    {
        

        public void Configure(EntityTypeBuilder<NotFileRepository> builder)
        {
            builder.ToTable(nameof(NotFileRepository));
            builder.HasKey(x => x.Id);
            builder.Ignore(x=>x.DomainEvents);

            builder.HasMany(x => x.FileGroups).WithOne().HasForeignKey(x => x.RepositoryGuid);

        }
    }
}
