using AutoMapper;
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
        private readonly IMapper _mapper;

        public SearchBoardItemsQueryHandler(
            IBoardItemRelationRepository relationRepository,
            IMapper mapper)
        {
            _relationRepository = relationRepository;
            _mapper = mapper;
        }

        public async Task<List<BoardItemSearchDto>> Handle(
            SearchBoardItemsQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
                return new List<BoardItemSearchDto>();

            var items = await _relationRepository.SearchBoardItemsAsync(
                request.BoardId,
                request.SearchTerm.Trim(),
                request.ExcludeItemId);

            return _mapper.Map<List<BoardItemSearchDto>>(items);
        }
    }
}
