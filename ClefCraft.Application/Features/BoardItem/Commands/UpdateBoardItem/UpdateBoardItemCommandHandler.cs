using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem
{
    public class UpdateBoardItemCommandHandler : IRequestHandler<UpdateBoardItemCommand, BoardItemByIdDto>
    {
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IMapper _mapper;

        public UpdateBoardItemCommandHandler(IBoardItemRepository boardItemRepository, IMapper mapper)
        {
            _boardItemRepository = boardItemRepository;
            _mapper = mapper;
        }

        public async Task<BoardItemByIdDto> Handle(UpdateBoardItemCommand request, CancellationToken cancellationToken)
        {
            // Fetch the existing board item
            var boardItem = await _boardItemRepository.GetBoardItemById(request.Id);

            if (boardItem == null)
            {
                throw new ApplicationException($"Board item with ID {request.Id} not found.");
            }

            boardItem.Title = request.Title ?? boardItem.Title;
            boardItem.Description = request.Description ?? boardItem.Description;
            boardItem.Status = request.Status ?? boardItem.Status;
            boardItem.BoardColumnId = request.BoardColumnId;

            await _boardItemRepository.UpdateBoardItem(boardItem);

            return _mapper.Map<BoardItemByIdDto>(boardItem);
        }
    }
}
