using ClefCraft.Application.Contracts.Persistence;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemRelations.Commands.DeleteRelation
{
    public class DeleteBoardItemRelationCommandHandler
        : IRequestHandler<DeleteBoardItemRelationCommand>
    {
        private readonly IBoardItemRelationRepository _relationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBoardItemRelationCommandHandler(
            IBoardItemRelationRepository relationRepository,
            IUnitOfWork unitOfWork)
        {
            _relationRepository = relationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            DeleteBoardItemRelationCommand request,
            CancellationToken cancellationToken)
        {
            await _relationRepository.DeleteAsync(request.RelationId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
