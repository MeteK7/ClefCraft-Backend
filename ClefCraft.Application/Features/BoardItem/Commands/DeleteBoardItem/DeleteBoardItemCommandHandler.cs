using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using MediatR;

namespace ClefCraft.Application.Features.BoardItem.Commands.DeleteBoardItem
{
    public class DeleteBoardItemCommandHandler : IRequestHandler<DeleteBoardItemCommand>
    {
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly ITaskLifecycleService _taskLifecycleService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBoardItemCommandHandler(
            IBoardItemRepository boardItemRepository,
            ICalendarEventRepository calendarEventRepository,
            ITaskLifecycleService taskLifecycleService,
            IUnitOfWork unitOfWork)
        {
            _boardItemRepository = boardItemRepository;
            _calendarEventRepository = calendarEventRepository;
            _taskLifecycleService = taskLifecycleService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteBoardItemCommand request, CancellationToken cancellationToken)
        {
            var boardItem = await _boardItemRepository.GetByIdAsync(request.Id);

            if (boardItem == null)
                throw new NotFoundException(nameof(Domain.BoardItem), request.Id);

            var linkedEvents = await _calendarEventRepository.GetWorkHistoryByItemIdAsync(request.Id);

            if (linkedEvents.Any())
                throw new BadRequestException(
                    $"Cannot delete board item {request.Id}: it is still linked to {linkedEvents.Count} calendar event(s). Unlink them first.");

            await _taskLifecycleService.DeleteAsync(request.Id);
            await _boardItemRepository.DeleteAsync(boardItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
