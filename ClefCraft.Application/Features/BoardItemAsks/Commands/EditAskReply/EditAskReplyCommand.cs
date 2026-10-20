using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.EditAskReply
{
    /// <summary>Changes the text of the caller's own reply. Replies can't be deleted.</summary>
    public class EditAskReplyCommand : IRequest<BoardItemAskDto>
    {
        public int ReplyId { get; set; }
        public string Body { get; set; } = null!;
    }
}
