using AutoMapper;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem
{
    public class CreateBoardItemCommandHandler : IRequestHandler<CreateBoardItemCommand, BoardItemDto>
    {
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly ITaskLifecycleService _taskLifecycleService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBoardItemCommandHandler(
            IBoardItemRepository boardItemRepository,
            IBoardAccessService boardAccessService,
            IMapper mapper,
            IUserService userService,
            ITaskLifecycleService taskLifecycleService,
            IUnitOfWork unitOfWork)
        {
            _boardItemRepository = boardItemRepository;
            _boardAccessService = boardAccessService;
            _mapper = mapper;
            _userService = userService;
            _taskLifecycleService = taskLifecycleService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BoardItemDto> Handle(CreateBoardItemCommand request, CancellationToken cancellationToken)
        {
            var userId = _userService.UserId;

            await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId, userId);

            var boardItem = new Domain.BoardItem
            {
                Title = request.Title,
                Description = request.Description,
                BoardColumnId = request.BoardColumnId,
                BoardId = request.BoardId,
                CreatedBy = userId,
                BoardItemStatus = new Domain.BoardItemStatus { StatusId = request.StatusId },
                BoardItemPriority = new Domain.BoardItemPriority { PriorityId = request.PriorityId }
            };

            await _boardItemRepository.AddBoardItem(boardItem);
            await _taskLifecycleService.EnsureCreatedAsync(boardItem.Id);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BoardItemDto>(boardItem);
        }
    }
}
