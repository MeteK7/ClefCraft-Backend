using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClefCraft.Persistence.Configurations
{
    public class BoardItemAskConfiguration : IEntityTypeConfiguration<BoardItemAsk>
    {
        public void Configure(EntityTypeBuilder<BoardItemAsk> builder)
        {
            builder.ToTable("BoardItemAsks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RequestedByUserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.RecipientUserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.CompletedByUserId)
                .HasMaxLength(100);

            builder.Property(x => x.Note)
                .IsRequired()
                .HasMaxLength(BoardItemAskLimits.NoteMaxLength);

            builder.Property(x => x.ResponseNote)
                .HasMaxLength(BoardItemAskLimits.ResponseNoteMaxLength);

            builder.Property(x => x.State)
                .IsRequired();

            // Both are checked on every update of the row, so two writers that loaded the same
            // version can't both commit: one of them gets a DbUpdateConcurrencyException.
            builder.Property(x => x.Revision)
                .IsConcurrencyToken();

            builder.Property(x => x.LastNudgedAt)
                .IsConcurrencyToken();

            builder.HasOne(x => x.BoardItem)
                .WithMany(x => x.Asks)
                .HasForeignKey(x => x.BoardItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.BoardItemId);
            builder.HasIndex(x => new { x.RecipientUserId, x.State });
            builder.HasIndex(x => new { x.RequestedByUserId, x.State });
        }
    }
}
