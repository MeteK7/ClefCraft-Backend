using ClefCraft.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClefCraft.Persistence.Configurations
{
    public class CalendarEventCollaboratorConfiguration : IEntityTypeConfiguration<CalendarEventCollaborator>
    {
        public void Configure(EntityTypeBuilder<CalendarEventCollaborator> builder)
        {
            builder.ToTable("CalendarEventCollaborators");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => new { x.CalendarEventId, x.UserId })
                .IsUnique();
        }
    }
}
