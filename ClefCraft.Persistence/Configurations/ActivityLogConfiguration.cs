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
    public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
    {
        public void Configure(EntityTypeBuilder<ActivityLog> builder)
        {
            builder.ToTable("ActivityLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.ActionType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.MetadataJson)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.Timestamp)
                .IsRequired();

            builder.HasIndex(x => new { x.EntityType, x.EntityId });
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Timestamp);
        }
    }
}
