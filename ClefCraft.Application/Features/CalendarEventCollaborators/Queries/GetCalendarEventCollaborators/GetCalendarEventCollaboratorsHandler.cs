using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;

namespace ClefCraft.Application.Features.CalendarEventCollaborators.Queries.GetCalendarEventCollaborators
{
    public class GetCalendarEventCollaboratorsHandler : IRequestHandler<GetCalendarEventCollaboratorsQuery, List<CalendarEventCollaboratorDto>>
    {
        private readonly ICalendarEventCollaboratorRepository _collaboratorRepository;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;

        public GetCalendarEventCollaboratorsHandler(
            ICalendarEventCollaboratorRepository collaboratorRepository,
            ICalendarAccessService calendarAccessService,
            IUserService userService)
        {
            _collaboratorRepository = collaboratorRepository;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
        }

        public async Task<List<CalendarEventCollaboratorDto>> Handle(GetCalendarEventCollaboratorsQuery request, CancellationToken cancellationToken)
        {
            // Any current collaborator (not just the owner) can see who else has access —
            // the list is visible, only granting/revoking is owner-only.
            await _calendarAccessService.EnsureCanAccessEventAsync(request.EventId, _userService.UserId);

            var collaborators = await _collaboratorRepository.GetByEventIdAsync(request.EventId);

            var userIds = collaborators.Select(c => c.UserId).Distinct().ToList();
            var users = await _userService.GetUsersByIds(userIds);

            return collaborators.Select(c =>
            {
                var user = users.FirstOrDefault(u => u.Id == c.UserId);
                return new CalendarEventCollaboratorDto
                {
                    UserId = c.UserId,
                    FullName = user != null ? $"{user.Firstname} {user.Lastname}" : "Unknown",
                    DateGranted = c.DateCreated
                };
            }).ToList();
        }
    }
}
