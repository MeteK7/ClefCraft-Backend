using AutoMapper;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
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
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public GetBoardItemByIdHandler(
            IBoardItemRepository boardItemRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IMapper mapper)
        {
            _boardItemRepository = boardItemRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<BoardItemByIdDto> Handle(GetBoardItemByIdQuery request, CancellationToken cancellationToken)
        {
            await _boardAccessService.EnsureBoardItemOwnedByUserAsync(request.Id, _userService.UserId);

            var boardItem = await _boardItemRepository.GetBoardItemById(request.Id);
            return _mapper.Map<BoardItemByIdDto>(boardItem);
        }
    }
}
