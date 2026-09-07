using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;

namespace ClefCraft.Application.Features.CalendarEventCollaborators.Commands.RemoveCalendarEventCollaborator
{
    public class RemoveCalendarEventCollaboratorCommandHandler : IRequestHandler<RemoveCalendarEventCollaboratorCommand>
    {
        private readonly ICalendarEventCollaboratorRepository _collaboratorRepository;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveCalendarEventCollaboratorCommandHandler(
            ICalendarEventCollaboratorRepository collaboratorRepository,
            ICalendarAccessService calendarAccessService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _collaboratorRepository = collaboratorRepository;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(RemoveCalendarEventCollaboratorCommand request, CancellationToken cancellationToken)
        {
            // Owner-only — revocation is a control the owner retains, not something a
            // collaborator can do to themselves or to each other.
            await _calendarAccessService.EnsureEventOwnedByUserAsync(request.EventId, _userService.UserId);

            // Deleting the CalendarEventCollaborator row only blocks future access checks —
            // existing Comment rows the removed collaborator authored are untouched, and
            // there's no live push to close a session they already have open (revocation
            // takes effect on their next request, same as any other authorization change).
            await _collaboratorRepository.RemoveAsync(request.EventId, request.CollaboratorUserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
