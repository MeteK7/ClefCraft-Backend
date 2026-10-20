using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.AddAskReply
{
    /// <summary>
    /// Posts a plain-text reply on an ask, by its requester or recipient. Changes nothing about the
    /// ask (state, revision, age), so it needs no expected revision.
    /// </summary>
    public class AddAskReplyCommand : IRequest<BoardItemAskDto>
    {
        public int AskId { get; set; }
        public string Body { get; set; } = null!;
    }
}
