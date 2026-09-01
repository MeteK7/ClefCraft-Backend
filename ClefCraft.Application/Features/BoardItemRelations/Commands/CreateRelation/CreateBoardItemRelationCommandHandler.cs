using AutoMapper;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.Commands.CreateRelation
{
    public class CreateBoardItemRelationCommandHandler
        : IRequestHandler<CreateBoardItemRelationCommand, RelationshipCardDto>
    {
        private readonly IBoardItemRelationRepository _relationRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateBoardItemRelationCommandHandler(
            IBoardItemRelationRepository relationRepository,
            IBoardItemRepository boardItemRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _relationRepository = relationRepository;
            _boardItemRepository = boardItemRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<RelationshipCardDto> Handle(
            CreateBoardItemRelationCommand request,
            CancellationToken cancellationToken)
        {
            if (request.SourceBoardItemId == request.TargetBoardItemId)
                throw new ApplicationException(
                    "An item cannot be related to itself.");

            var source =
                await _boardItemRepository.GetBoardItemById(
                    request.SourceBoardItemId);

            if (source == null)
                throw new ApplicationException(
                    $"Board item {request.SourceBoardItemId} was not found.");

            var target =
                await _boardItemRepository.GetBoardItemById(
                    request.TargetBoardItemId);

            if (target == null)
                throw new ApplicationException(
                    $"Board item {request.TargetBoardItemId} was not found.");

            var userId = _userService.UserId;
            await _boardAccessService.EnsureBoardOwnedByUserAsync(source.BoardId, userId);
            await _boardAccessService.EnsureBoardOwnedByUserAsync(target.BoardId, userId);

            var relationType =
                (BoardItemRelationType)request.RelationType;

            var exists =
                await _relationRepository.ExistsAsync(
                    request.SourceBoardItemId,
                    request.TargetBoardItemId,
                    relationType);

            if (exists)
                throw new ApplicationException(
                    "Relationship already exists.");

            var relation = new BoardItemRelation
            {
                SourceBoardItemId = request.SourceBoardItemId,
                TargetBoardItemId = request.TargetBoardItemId,
                RelationType = relationType
            };

            await _relationRepository.AddAsync(relation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<RelationshipCardDto>(target);
            dto.RelationId = relation.Id;

            return dto;
        }
    }
}