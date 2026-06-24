using ClefCraft.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Configurations
{
    public class EntitySnapshotConfiguration : IEntityTypeConfiguration<EntitySnapshot>
    {
        public void Configure(EntityTypeBuilder<EntitySnapshot> builder)
        {
            builder.ToTable("EntitySnapshots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.SnapshotJson)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.EntityType, x.EntityId });
        }
    }
}
