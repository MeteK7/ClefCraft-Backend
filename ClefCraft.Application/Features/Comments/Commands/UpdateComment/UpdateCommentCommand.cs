using MediatR;

namespace ClefCraft.Application.Features.Comments.Commands.UpdateComment
{
    public class UpdateCommentCommand : IRequest<CommentDto>
    {
        public int Id { get; set; }
        public string BodyHtml { get; set; } = default!;
        public List<string> MentionedUserIds { get; set; } = new();
    }
}
