using MediatR;

namespace ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember
{
    public class RemoveBoardMemberCommand : IRequest
    {
        public int BoardId { get; set; }

        /// <summary>The member being removed.</summary>
        public string UserId { get; set; }

        /// <summary>The caller — set by the controller, must be the board's owner.</summary>
        public string RequestingUserId { get; set; }
    }
}
