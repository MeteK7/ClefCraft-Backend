using ClefCraft.Application.Common.Models;
using ClefCraft.Application.Contracts.ActivityLogs;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Exceptions;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity
{
    public class GetActivityLogForEntityHandler : IRequestHandler<GetActivityLogForEntityQuery, PagedResult<ActivityLogEntryDto>>
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;

        public GetActivityLogForEntityHandler(
            IActivityLogRepository activityLogRepository,
            IBoardAccessService boardAccessService,
            IUserService userService)
        {
            _activityLogRepository = activityLogRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
        }

        private const string AssigneeFieldName = "AssigneeId";

        // null for "no assignee" (nothing to show); "Unknown user" when the account no longer exists.
        private static string? AssigneeDisplayName(string? userId, Dictionary<string, string> fullNames) =>
            string.IsNullOrEmpty(userId)
                ? null
                : fullNames.TryGetValue(userId, out var name) ? name : "Unknown user";

        public async Task<PagedResult<ActivityLogEntryDto>> Handle(GetActivityLogForEntityQuery request, CancellationToken cancellationToken)
        {
            var validator = new GetActivityLogForEntityValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid activity log request", validationResult);

            // AllowedEntityTypes only contains "BoardItem" today (enforced by the validator
            // above). Dispatch by type so a future addition to AllowedEntityTypes fails closed
            // here instead of silently skipping the ownership check.
            switch (request.EntityType)
            {
                case "BoardItem":
                    await _boardAccessService.EnsureBoardItemOwnedByUserAsync(request.EntityId, _userService.UserId);
                    break;
                default:
                    throw new ForbiddenAccessException();
            }

            var skip = (request.PageNumber - 1) * request.PageSize;

            var logs = await _activityLogRepository.GetByEntityAsync(request.EntityType, request.EntityId, skip, request.PageSize);
            var totalCount = await _activityLogRepository.CountByEntityAsync(request.EntityType, request.EntityId);

            // Parsed once, in the same order as logs.
            var changesPerLog = logs.Select(l => ActivityMetadataParser.Parse(l.MetadataJson)).ToList();

            var assigneeChanges = changesPerLog
                .SelectMany(changes => changes)
                .Where(c => c.FieldName == AssigneeFieldName)
                .ToList();

            // Actors and assignees (past or present, board member or not) in one lookup.
            var userIds = logs
                .Select(l => l.UserId)
                .Concat(assigneeChanges.SelectMany(c => new[] { c.OldValue, c.NewValue }))
                .Where(userId => !string.IsNullOrEmpty(userId))
                .Select(userId => userId!)
                .Distinct()
                .ToList();

            var users = await _userService.GetUsersByIds(userIds);
            var fullNames = users.ToDictionary(u => u.Id, u => $"{u.Firstname} {u.Lastname}");

            foreach (var change in assigneeChanges)
            {
                change.OldDisplayValue = AssigneeDisplayName(change.OldValue, fullNames);
                change.NewDisplayValue = AssigneeDisplayName(change.NewValue, fullNames);
            }

            var items = logs.Select((l, index) =>
            {
                var user = users.FirstOrDefault(u => u.Id == l.UserId);

                return new ActivityLogEntryDto
                {
                    Id = l.Id,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    ActionType = l.ActionType,
                    Timestamp = l.Timestamp,
                    ActorUserId = l.UserId,
                    ActorFullName = user != null ? $"{user.Firstname} {user.Lastname}" : "Unknown",
                    Changes = changesPerLog[index]
                };
            }).ToList();

            return new PagedResult<ActivityLogEntryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
