using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClefCraft.Persistence.Configurations
{
    public class BoardItemAskReplyConfiguration : IEntityTypeConfiguration<BoardItemAskReply>
    {
        public void Configure(EntityTypeBuilder<BoardItemAskReply> builder)
        {
            builder.ToTable("BoardItemAskReplies");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AuthorUserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Body)
                .IsRequired()
                .HasMaxLength(BoardItemAskLimits.ReplyMaxLength);

            builder.HasOne(x => x.BoardItemAsk)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.BoardItemAskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.BoardItemAskId);
        }
    }
}
