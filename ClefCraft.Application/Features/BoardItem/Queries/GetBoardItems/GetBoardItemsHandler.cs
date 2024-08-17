using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems
{
    public class GetBoardItemsHandler : IRequestHandler<GetBoardItemsQuery, List<BoardColumnDto>>
    {
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IMapper _mapper;

        public GetBoardItemsHandler(IBoardItemRepository boardItemRepository, IMapper mapper)
        {
            _boardItemRepository = boardItemRepository;
            _mapper = mapper;
        }
        public async Task<List<BoardColumnDto>> Handle(GetBoardItemsQuery request, CancellationToken cancellationToken)
        {
            var columns = await _boardItemRepository.GetBoardColumnsWithBoardItems();
            return _mapper.Map<List<BoardColumnDto>>(columns);
        }
    }
}
