using AutoMapper;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Domain;
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
        private readonly IStatusRepository _statusRepository;
        private readonly IPriorityRepository _priorityRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public UpdateBoardItemCommandHandler(
            IBoardItemRepository boardItemRepository,
            IStatusRepository statusRepository,
            IPriorityRepository priorityRepository,
            ITagRepository tagRepository,
            IMapper mapper,
            IUserService userService)
        {
            _boardItemRepository = boardItemRepository;
            _statusRepository = statusRepository;
            _priorityRepository = priorityRepository;
            _tagRepository = tagRepository;
            _mapper = mapper;
            _userService = userService;
        }

        public async Task<BoardItemByIdDto> Handle(UpdateBoardItemCommand request, CancellationToken cancellationToken)
        {
            var boardItem = await _boardItemRepository.GetBoardItemById(request.Id);

            if (boardItem == null)
            {
                throw new ApplicationException($"Board item with ID {request.Id} not found.");
            }

            boardItem.Title = request.Title ?? boardItem.Title;
            boardItem.Description = request.Description ?? boardItem.Description;

            if (request.StatusId.HasValue)
            {
                boardItem.Status = await _statusRepository.GetByIdAsync(request.StatusId.Value);
            }

            if (request.PriorityId.HasValue)
            {
                boardItem.Priority = await _priorityRepository.GetByIdAsync(request.PriorityId.Value);
            }

            if (request.TagIds != null)
            {
                boardItem.BoardItemTags = (await _tagRepository.GetTagsByIdsAsync(request.TagIds))
                    .Select(tag => new BoardItemTag { BoardItem = boardItem, Tag = tag })
                    .ToList();
            }

            boardItem.Assignee = request.Assignee ?? boardItem.Assignee;
            boardItem.DueDate = request.DueDate ?? boardItem.DueDate;
            boardItem.EstimatedTime = request.EstimatedTime ?? boardItem.EstimatedTime;
            boardItem.TimeSpent = request.TimeSpent ?? boardItem.TimeSpent;
            boardItem.BoardColumnId = request.BoardColumnId;

            await _boardItemRepository.UpdateBoardItem(boardItem);

            return _mapper.Map<BoardItemByIdDto>(boardItem);
        }
    }
}
