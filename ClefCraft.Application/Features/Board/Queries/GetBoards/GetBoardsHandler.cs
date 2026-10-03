using ClefCraft.Application.Contracts.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Board.Queries.GetBoards
{
    public class GetBoardsHandler : IRequestHandler<GetBoardsQuery, List<BoardDto>>
    {
        private readonly IBoardRepository _boardRepository;

        public GetBoardsHandler(IBoardRepository boardRepository)
        {
            _boardRepository = boardRepository;
        }
        public async Task<List<BoardDto>> Handle(GetBoardsQuery request, CancellationToken cancellationToken)
        {
            var boards = await _boardRepository.GetBoards(request.UserId);
            return boards.Select(BoardMapper.ToDto).ToList();
        }
    }
}
