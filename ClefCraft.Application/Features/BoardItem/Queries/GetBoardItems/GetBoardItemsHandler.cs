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
            if (request.BoardId.HasValue)
            {
                var columns = await _boardItemRepository.GetBoardColumnsWithBoardItems(request.BoardId.Value);
                return _mapper.Map<List<BoardColumnDto>>(columns);
            }
            else
            {
                // Fetch all board columns and items if no specific board is selected
                var allColumns = await _boardItemRepository.GetAllBoardColumnsWithItems();
                return _mapper.Map<List<BoardColumnDto>>(allColumns);
            }
        }
    }
}
