using AutoMapper;
using ClefCraft.Application.Contracts.Identity;
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
        private readonly IUserService _userService;

        public GetBoardItemsHandler(IBoardItemRepository boardItemRepository, IMapper mapper, IUserService userService)
        {
            _boardItemRepository = boardItemRepository;
            _mapper = mapper;
            _userService = userService;
        }

        public async Task<List<BoardColumnDto>> Handle(GetBoardItemsQuery request, CancellationToken cancellationToken)
        {
            var columns = request.BoardId.HasValue
                ? await _boardItemRepository.GetBoardColumnsWithBoardItems(request.BoardId.Value)
                : await _boardItemRepository.GetAllBoardColumnsWithItems();

            var mappedColumns = _mapper.Map<List<BoardColumnDto>>(columns);

            foreach (var column in mappedColumns)
            {
                foreach (var item in column.BoardItems)
                {
                    if (!string.IsNullOrEmpty(item.CreatedBy))
                    {
                        var creator = await _userService.GetUser(item.CreatedBy);
                        item.CreatedByFullName = $"{creator.Firstname} {creator.Lastname}";
                    }

                    if (!string.IsNullOrEmpty(item.ModifiedBy))
                    {
                        var modifier = await _userService.GetUser(item.ModifiedBy);
                        item.ModifiedByFullName = $"{modifier.Firstname} {modifier.Lastname}";
                    }
                }
            }

            return mappedColumns;
        }
    }
}
