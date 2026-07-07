using ClefCraft.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClefCraft.Persistence.Configurations
{
    public class BoardItemRelationConfiguration
        : IEntityTypeConfiguration<BoardItemRelation>
    {
        public void Configure(EntityTypeBuilder<BoardItemRelation> builder)
        {
            builder.ToTable("BoardItemRelations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RelationType)
                   .IsRequired();

            builder.HasOne(x => x.SourceBoardItem)
                   .WithMany(x => x.OutgoingRelations)
                   .HasForeignKey(x => x.SourceBoardItemId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.TargetBoardItem)
                   .WithMany(x => x.IncomingRelations)
                   .HasForeignKey(x => x.TargetBoardItemId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.SourceBoardItemId,
                x.TargetBoardItemId,
                x.RelationType
            })
            .IsUnique();
        }
    }
}