using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.ChangeAskState
{
    public class ChangeAskStateCommand : IRequest<BoardItemAskDto>
    {
        public int AskId { get; set; }
        public int ExpectedRevision { get; set; }

        /// <summary>Accept, Decline, Complete, Reopen or Cancel.</summary>
        public BoardItemAskAction Action { get; set; }

        /// <summary>Optional: the recipient's reason when declining.</summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Complete only, optional: "Send & mark done". The reply is posted and the ask completed in
        /// one save; from Pending the ask is accepted first, in the same save.
        /// </summary>
        public string? ReplyBody { get; set; }
    }
}
