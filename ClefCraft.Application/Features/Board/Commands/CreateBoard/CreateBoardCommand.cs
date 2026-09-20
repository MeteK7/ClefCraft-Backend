using ClefCraft.Application.Features.Board.Queries.GetBoards;
using MediatR;

namespace ClefCraft.Application.Features.Board.Commands.CreateBoard
{
    /// <summary>
    /// Creates a new board owned by the caller. OwnerUserId is deliberately not a
    /// settable field here — the handler always sources it from IUserService, never
    /// from the request body (mirrors AddBoardMemberCommand's RequestingUserId).
    /// </summary>
    public class CreateBoardCommand : IRequest<BoardDto>
    {
        public string Title { get; set; }
    }
}
