using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClefCraft.Domain;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.Contracts.Persistence;

namespace ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem
{
    public class CreateBoardItemCommandHandler : IRequestHandler<CreateBoardItemCommand, BoardItemDto>
    {
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IMapper _mapper;

        public CreateBoardItemCommandHandler(IBoardItemRepository boardItemRepository, IMapper mapper)
        {
            _boardItemRepository = boardItemRepository;
            _mapper = mapper;
        }

        public async Task<BoardItemDto> Handle(CreateBoardItemCommand request, CancellationToken cancellationToken)
        {
            var boardItem = new Domain.BoardItem
            {
                Title = request.Title,
                Description = request.Description,
                BoardColumnId = request.BoardColumnId
            };



            await _boardItemRepository.AddBoardItem(boardItem);

            return _mapper.Map<BoardItemDto>(boardItem);
        }
    }
}
