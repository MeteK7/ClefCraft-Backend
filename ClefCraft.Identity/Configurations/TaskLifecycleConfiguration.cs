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
    public class TaskLifecycleConfiguration : IEntityTypeConfiguration<TaskLifecycle>
    {
        public void Configure(EntityTypeBuilder<TaskLifecycle> builder)
        {
            builder.ToTable("TaskLifecycles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.BoardItemId)
                .IsRequired();

            builder.HasIndex(x => x.BoardItemId)
                .IsUnique();

            builder.Property(x => x.CreatedAt).IsRequired();

            builder.Property(x => x.ReopenCount).HasDefaultValue(0);
            builder.Property(x => x.StatusChangeCount).HasDefaultValue(0);
            builder.Property(x => x.AssigneeChangeCount).HasDefaultValue(0);
        }
    }
}
