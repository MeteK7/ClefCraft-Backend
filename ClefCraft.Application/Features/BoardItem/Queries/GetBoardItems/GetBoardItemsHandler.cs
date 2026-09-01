using AutoMapper;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Domain;
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
        private readonly IBoardAccessService _boardAccessService;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public GetBoardItemsHandler(
            IBoardItemRepository boardItemRepository,
            IBoardAccessService boardAccessService,
            IMapper mapper,
            IUserService userService)
        {
            _boardItemRepository = boardItemRepository;
            _boardAccessService = boardAccessService;
            _mapper = mapper;
            _userService = userService;
        }

        public async Task<List<BoardColumnDto>> Handle(GetBoardItemsQuery request, CancellationToken cancellationToken)
        {
            List<ClefCraft.Domain.BoardColumn> columns;

            if (request.BoardId.HasValue)
            {
                await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId.Value, _userService.UserId);
                columns = await _boardItemRepository.GetBoardColumnsWithBoardItems(request.BoardId.Value);
            }
            else
            {
                columns = await _boardItemRepository.GetAllBoardColumnsWithItems(_userService.UserId);
            }

            var mappedColumns = _mapper.Map<List<BoardColumnDto>>(columns);

            // Collect ALL user IDs (distinct)
            var userIds = mappedColumns
                .SelectMany(c => c.BoardItems)
                .SelectMany(i => new[]
                {
            i.CreatedBy,
            i.ModifiedBy,
            i.AssigneeId
                })
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            // Fetch all users in ONE query
            var users = await _userService.GetUsersByIds(userIds);

            // Convert to dictionary for O(1) lookup
            var userDictionary = users.ToDictionary(u => u.Id);

            // Map FullNames without extra DB calls
            foreach (var column in mappedColumns)
            {
                foreach (var item in column.BoardItems)
                {
                    if (!string.IsNullOrEmpty(item.CreatedBy)
                        && userDictionary.TryGetValue(item.CreatedBy, out var creator))
                    {
                        item.CreatedByFullName = $"{creator.Firstname} {creator.Lastname}";
                    }

                    if (!string.IsNullOrEmpty(item.ModifiedBy)
                        && userDictionary.TryGetValue(item.ModifiedBy, out var modifier))
                    {
                        item.ModifiedByFullName = $"{modifier.Firstname} {modifier.Lastname}";
                    }

                    if (!string.IsNullOrEmpty(item.AssigneeId)
                        && userDictionary.TryGetValue(item.AssigneeId, out var assignee))
                    {
                        item.AssigneeFirstName = assignee.Firstname;
                        item.AssigneeLastName = assignee.Lastname;
                    }
                }
            }

            return mappedColumns;
        }
    }
}
