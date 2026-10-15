using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.UpdateAsk
{
    /// <summary>
    /// Replaces an ask's terms (note and due date) and its requirement. Send the current values for
    /// whatever isn't changing. Changing the terms of an Accepted ask asks again: it goes back to
    /// Pending. Changing IsRequired never changes the state.
    /// </summary>
    public class UpdateAskCommand : IRequest<BoardItemAskDto>
    {
        public int AskId { get; set; }
        public int ExpectedRevision { get; set; }
        public string Note { get; set; } = null!;
        public DateTime? DueDate { get; set; }
        public bool IsRequired { get; set; }

        /// <summary>Optional, recorded in the history when IsRequired goes from true to false.</summary>
        public string? WaiveReason { get; set; }
    }
}
