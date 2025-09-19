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
    internal class NotFileEntityConfiguration:IEntityTypeConfiguration<NotFile>
    {
       

        public void Configure(EntityTypeBuilder<NotFile> builder)
        {

            builder.HasKey(x=>x.Id);

            builder.Ignore(x => x.DomainEvents);

            builder.ToTable("NotFile");

            builder.OwnsOne(en => en.ObjectMap, en => { 
                en.Property(p => p.ObjectKey).HasColumnType("varchar").HasColumnName("ObjectKey");
                en.Property(p => p.PhysicalName).HasColumnType("varchar").HasColumnName("PhysicalName");
            });

        }
    }
}
