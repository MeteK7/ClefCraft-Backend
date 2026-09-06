using ClefCraft.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClefCraft.Persistence.Configurations
{
    public class CommentMentionConfiguration : IEntityTypeConfiguration<CommentMention>
    {
        public void Configure(EntityTypeBuilder<CommentMention> builder)
        {
            builder.ToTable("CommentMentions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MentionedUserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.CommentId);
            builder.HasIndex(x => x.MentionedUserId);
        }
    }
}
