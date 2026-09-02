using ClefCraft.Application.Features.BoardMembers.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardMembers.Commands.AddMember
{
    public class AddBoardMemberCommand : IRequest<BoardMemberDto>
    {
        public int BoardId { get; set; }

        /// <summary>The user being added as a member.</summary>
        public string UserId { get; set; }

        /// <summary>The caller — set by the controller, must be the board's owner.</summary>
        public string RequestingUserId { get; set; }
    }
}
