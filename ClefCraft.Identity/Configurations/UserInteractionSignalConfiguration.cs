using ClefCraft.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Identity.Configurations
{
    public class UserInteractionSignalConfiguration : IEntityTypeConfiguration<UserInteractionSignal>
    {
        public void Configure(EntityTypeBuilder<UserInteractionSignal> builder)
        {
            builder.ToTable("UserInteractionSignals");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.SignalType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Value)
                .IsRequired();

            builder.Property(x => x.Timestamp)
                .IsRequired();

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => new { x.EntityType, x.EntityId });
        }
    }
}
