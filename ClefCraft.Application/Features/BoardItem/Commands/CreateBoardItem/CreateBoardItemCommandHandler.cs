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
using ClefCraft.Application.Contracts.Identity;

namespace ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem
{
    public class CreateBoardItemCommandHandler : IRequestHandler<CreateBoardItemCommand, BoardItemDto>
    {
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public CreateBoardItemCommandHandler(IBoardItemRepository boardItemRepository, IMapper mapper, IUserService userService)
        {
            _boardItemRepository = boardItemRepository;
            _mapper = mapper;
            _userService = userService;
        }

        public async Task<BoardItemDto> Handle(CreateBoardItemCommand request, CancellationToken cancellationToken)
        {
            var userId = _userService.UserId;
            var boardItem = new Domain.BoardItem
            {
                Title = request.Title,
                Description = request.Description,
                BoardColumnId = request.BoardColumnId,
                BoardId = request.BoardId,
                CreatedBy = userId
            };

            await _boardItemRepository.AddBoardItem(boardItem);

            return _mapper.Map<BoardItemDto>(boardItem);
        }
    }
}
