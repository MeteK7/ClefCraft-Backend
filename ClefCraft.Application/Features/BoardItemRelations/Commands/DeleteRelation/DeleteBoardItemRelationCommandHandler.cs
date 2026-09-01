using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemRelations.Commands.DeleteRelation
{
    public class DeleteBoardItemRelationCommandHandler
        : IRequestHandler<DeleteBoardItemRelationCommand>
    {
        private readonly IBoardItemRelationRepository _relationRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBoardItemRelationCommandHandler(
            IBoardItemRelationRepository relationRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _relationRepository = relationRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            DeleteBoardItemRelationCommand request,
            CancellationToken cancellationToken)
        {
            var relation = await _relationRepository.GetByIdAsync(request.RelationId);

            if (relation == null)
                throw new NotFoundException(nameof(BoardItemRelation), request.RelationId);

            var userId = _userService.UserId;
            await _boardAccessService.EnsureBoardItemOwnedByUserAsync(relation.SourceBoardItemId, userId);
            await _boardAccessService.EnsureBoardItemOwnedByUserAsync(relation.TargetBoardItemId, userId);

            await _relationRepository.DeleteAsync(request.RelationId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
