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
    }
}
