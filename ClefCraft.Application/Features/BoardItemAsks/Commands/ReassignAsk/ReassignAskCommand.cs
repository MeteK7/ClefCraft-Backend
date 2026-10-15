using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.ReassignAsk
{
    /// <summary>
    /// Asks someone else instead: cancels an open ask (Withdrawn) and creates a Pending one for the
    /// new recipient, in one save. The new ask keeps the requester and takes over the requirement.
    /// Returns the new ask.
    /// </summary>
    public class ReassignAskCommand : IRequest<BoardItemAskDto>
    {
        public int AskId { get; set; }
        public int ExpectedRevision { get; set; }
        public string NewRecipientUserId { get; set; } = null!;

        /// <summary>Null keeps the current note.</summary>
        public string? Note { get; set; }

        /// <summary>Null keeps the current due date.</summary>
        public DateTime? DueDate { get; set; }
    }
}
