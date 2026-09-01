using AutoMapper;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.Queries.SearchBoardItems
{
    public class SearchBoardItemsQueryHandler
        : IRequestHandler<SearchBoardItemsQuery, List<BoardItemSearchDto>>
    {
        private readonly IBoardItemRelationRepository _relationRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public SearchBoardItemsQueryHandler(
            IBoardItemRelationRepository relationRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IMapper mapper)
        {
            _relationRepository = relationRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<List<BoardItemSearchDto>> Handle(
            SearchBoardItemsQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
                return new List<BoardItemSearchDto>();

            await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId, _userService.UserId);

            var items = await _relationRepository.SearchBoardItemsAsync(
                request.BoardId,
                request.SearchTerm.Trim(),
                request.ExcludeItemId);

            return _mapper.Map<List<BoardItemSearchDto>>(items);
        }
    }
}
