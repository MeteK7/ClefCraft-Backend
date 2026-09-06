using MediatR;

namespace ClefCraft.Application.Features.Comments.Commands.CreateComment
{
    public class CreateCommentCommand : IRequest<CommentDto>
    {
        public string EntityType { get; set; } = default!;
        public int EntityId { get; set; }
        public int? ParentCommentId { get; set; }
        public string BodyHtml { get; set; } = default!;
        public List<string> MentionedUserIds { get; set; } = new();
    }
}
