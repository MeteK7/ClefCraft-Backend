using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.CreateAsk
{
    public class CreateAskCommand : IRequest<BoardItemAskDto>
    {
        public int BoardItemId { get; set; }
        public string RecipientUserId { get; set; } = null!;
        public string Note { get; set; } = null!;
        public DateTime? DueDate { get; set; }
        public bool IsRequired { get; set; }

        /// <summary>
        /// "Ask again": a Declined or Cancelled ask on the same item that this one follows up. The
        /// new ask keeps that ask's requester, and when both are required the requirement moves to
        /// the new ask.
        /// </summary>
        public int? ReplacesAskId { get; set; }
    }
}
