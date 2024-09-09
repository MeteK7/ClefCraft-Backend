using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById
{
    public class GetBoardItemByIdHandler : IRequestHandler<GetBoardItemByIdQuery, BoardItemByIdDto>
    {
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IMapper _mapper;

        public GetBoardItemByIdHandler(IBoardItemRepository boardItemRepository, IMapper mapper)
        {
            _boardItemRepository = boardItemRepository;
            _mapper = mapper;
        }

        public async Task<BoardItemByIdDto> Handle(GetBoardItemByIdQuery request, CancellationToken cancellationToken)
        {
            var boardItem = await _boardItemRepository.GetBoardItemById(request.Id);
            return _mapper.Map<BoardItemByIdDto>(boardItem);
        }
    }
}
