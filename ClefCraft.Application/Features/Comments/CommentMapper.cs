using ClefCraft.Application.Models.Identity;
using ClefCraft.Domain;

namespace ClefCraft.Application.Features.Comments
{
    public static class CommentMapper
    {
        public static CommentDto ToDto(Comment comment, User? author, List<string> mentionedUserIds)
        {
            return new CommentDto
            {
                Id = comment.Id,
                EntityType = comment.EntityType,
                EntityId = comment.EntityId,
                ParentCommentId = comment.ParentCommentId,
                // Tombstoned comments never expose their body or mentions, even though the
                // row (and its place among replies) is kept.
                BodyHtml = comment.IsDeleted ? null : comment.BodyHtml,
                IsDeleted = comment.IsDeleted,
                AuthorUserId = comment.CreatedBy ?? string.Empty,
                AuthorFullName = author != null ? $"{author.Firstname} {author.Lastname}" : "Unknown",
                DateCreated = comment.DateCreated,
                DateModified = comment.DateModified,
                // DateCreated and DateModified are stamped with the exact same instant on
                // creation (ClefCraftDatabaseContext.SaveChangesAsync sets both from one
                // shared utcNow), so equality means "never edited" and only a later,
                // strictly-greater DateModified means a real edit happened.
                IsEdited = comment.DateModified.HasValue && comment.DateCreated.HasValue
                    && comment.DateModified > comment.DateCreated,
                MentionedUserIds = comment.IsDeleted ? new List<string>() : mentionedUserIds
            };
        }
    }
}
