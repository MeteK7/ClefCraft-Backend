using AutoMapper;
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
        private readonly IMapper _mapper;

        public GetBoardsHandler(IBoardRepository boardRepository, IMapper mapper)
        {
            _boardRepository = boardRepository;
            _mapper = mapper;
        }
        public async Task<List<BoardDto>> Handle(GetBoardsQuery request, CancellationToken cancellationToken)
        {
            var boards = await _boardRepository.GetBoards();
            return _mapper.Map<List<BoardDto>>(boards);
        }
    }
}
