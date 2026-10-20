using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.SetAskExpectsAnswer
{
    /// <summary>Turns "Expects an answer" on or off, as Make required / Waive requirement do for IsRequired.</summary>
    public class SetAskExpectsAnswerCommand : IRequest<BoardItemAskDto>
    {
        public int AskId { get; set; }
        public int ExpectedRevision { get; set; }
        public bool ExpectsAnswer { get; set; }
    }
}
