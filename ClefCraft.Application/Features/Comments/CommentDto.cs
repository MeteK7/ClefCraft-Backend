namespace ClefCraft.Application.Features.Comments
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = default!;
        public int EntityId { get; set; }
        public int? ParentCommentId { get; set; }
        public string? BodyHtml { get; set; }
        public bool IsDeleted { get; set; }
        public string AuthorUserId { get; set; } = default!;
        public string AuthorFullName { get; set; } = default!;
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public bool IsEdited { get; set; }
        public List<string> MentionedUserIds { get; set; } = new();
    }
}
