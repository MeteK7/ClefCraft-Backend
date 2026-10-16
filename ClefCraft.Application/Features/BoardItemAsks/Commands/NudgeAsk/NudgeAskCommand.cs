using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.NudgeAsk
{
    /// <summary>
    /// Reminds the recipient of a Pending ask. At most once per ask in any 24 hours. Changes no
    /// state, revision or age, so a recipient's answer to the version they saw is still accepted.
    /// </summary>
    public class NudgeAskCommand : IRequest<BoardItemAskDto>
    {
        public int AskId { get; set; }
    }
}
