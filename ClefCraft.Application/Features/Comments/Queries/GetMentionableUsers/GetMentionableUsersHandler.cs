using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using MediatR;

namespace ClefCraft.Application.Features.Comments.Queries.GetMentionableUsers
{
    public class GetMentionableUsersHandler : IRequestHandler<GetMentionableUsersQuery, List<MentionableUserDto>>
    {
        private readonly IBoardAccessService _boardAccessService;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly ICalendarEventCollaboratorRepository _collaboratorRepository;
        private readonly IUserService _userService;

        public GetMentionableUsersHandler(
            IBoardAccessService boardAccessService,
            ICalendarAccessService calendarAccessService,
            IBoardItemRepository boardItemRepository,
            ICalendarEventRepository calendarEventRepository,
            IBoardMemberRepository boardMemberRepository,
            ICalendarEventCollaboratorRepository collaboratorRepository,
            IUserService userService)
        {
            _boardAccessService = boardAccessService;
            _calendarAccessService = calendarAccessService;
            _boardItemRepository = boardItemRepository;
            _calendarEventRepository = calendarEventRepository;
            _boardMemberRepository = boardMemberRepository;
            _collaboratorRepository = collaboratorRepository;
            _userService = userService;
        }

        public async Task<List<MentionableUserDto>> Handle(GetMentionableUsersQuery request, CancellationToken cancellationToken)
        {
            if (!AllowedEntityTypes.Values.Contains(request.EntityType))
                throw new BadRequestException($"{request.EntityType} is not a supported entity type");

            var callerId = _userService.UserId;

            await CommentAccess.EnsureCanAccessAsync(
                request.EntityType, request.EntityId, callerId,
                _boardAccessService, _calendarAccessService);

            var candidateIds = new HashSet<string>();

            if (request.EntityType == "BoardItem")
            {
                var item = await _boardItemRepository.GetByIdReadOnlyAsync(request.EntityId);
                if (item == null) throw new NotFoundException(nameof(Domain.BoardItem), request.EntityId);

                var members = await _boardMemberRepository.GetByBoardIdAsync(item.BoardId);
                foreach (var m in members) candidateIds.Add(m.UserId);
            }
            else // CalendarEvent
            {
                var calendarEvent = await _calendarEventRepository.GetByIdReadOnlyAsync(request.EntityId);
                if (calendarEvent == null) throw new NotFoundException(nameof(Domain.CalendarEvent), request.EntityId);

                candidateIds.Add(calendarEvent.UserId);

                if (callerId == calendarEvent.UserId)
                {
                    // Only the owner can grant new access, so only the owner sees a broad
                    // "people I could invite" discovery pool — this list is suggestions, not
                    // access; mentioning someone from it is what actually grants them
                    // CalendarEventCollaborator access (see CalendarMentionAccess). Deliberately
                    // org-wide via GetAssignees (all users, no role coupling) — not board-scoped:
                    // calendar sharing is independent of board membership by design, so a user on
                    // no boards at all must still be able to discover and share with anyone, not
                    // be locked out of the feature. GetEmployees() was considered but rejected:
                    // it's scoped to the "Employee" identity role, so it silently excludes any
                    // account outside that role.
                    var assignees = await _userService.GetAssignees();
                    foreach (var a in assignees) candidateIds.Add(a.Id);
                }
                else
                {
                    // A non-owner (an already-granted collaborator) can only mention people
                    // already in the conversation — the same set CalendarMentionAccess would
                    // treat as valid — never the owner's wider board network. Otherwise the
                    // dropdown itself would invite the privilege escalation the mention policy
                    // is guarding against.
                    var collaborators = await _collaboratorRepository.GetByEventIdAsync(request.EntityId);
                    foreach (var c in collaborators) candidateIds.Add(c.UserId);
                }
            }

            candidateIds.Remove(callerId);

            var users = await _userService.GetUsersByIds(candidateIds.ToList());

            return users
                .Select(u => new MentionableUserDto { UserId = u.Id, FullName = $"{u.Firstname} {u.Lastname}" })
                .OrderBy(u => u.FullName)
                .ToList();
        }
    }
}
