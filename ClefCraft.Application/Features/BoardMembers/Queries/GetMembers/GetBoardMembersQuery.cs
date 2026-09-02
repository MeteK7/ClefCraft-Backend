using ClefCraft.Application.Features.BoardMembers.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardMembers.Queries.GetMembers
{
    public class GetBoardMembersQuery : IRequest<List<BoardMemberDto>>
    {
        public int BoardId { get; set; }
        public string UserId { get; set; }
    }
}
