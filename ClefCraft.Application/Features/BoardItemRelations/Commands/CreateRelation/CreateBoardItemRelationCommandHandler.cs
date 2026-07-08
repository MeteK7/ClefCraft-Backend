using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemRelations.Commands.CreateRelation
{
    public class CreateBoardItemRelationCommandHandler
        : IRequestHandler<CreateBoardItemRelationCommand, int>
    {
        private readonly IBoardItemRelationRepository _relationRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBoardItemRelationCommandHandler(
            IBoardItemRelationRepository relationRepository,
            IBoardItemRepository boardItemRepository,
            IUnitOfWork unitOfWork)
        {
            _relationRepository = relationRepository;
            _boardItemRepository = boardItemRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> Handle(
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

            return relation.Id;
        }
    }
}