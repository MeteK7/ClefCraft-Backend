using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks
{
    public enum AskDirection
    {
        /// <summary>"Asked of you": open asks addressed to the caller.</summary>
        Received = 0,

        /// <summary>"Asks you sent": open asks the caller made, plus those declined in the last 14 days.</summary>
        Sent = 1
    }

    /// <summary>
    /// The caller's asks across the boards they are currently a member of. The caller is always the
    /// authenticated user (IUserService.UserId); there is no user id to supply.
    /// </summary>
    public class GetMyAsksQuery : IRequest<List<BoardItemAskDto>>
    {
        public AskDirection Direction { get; set; }
    }
}
