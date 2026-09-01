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

            var userIds = logs
                .Select(l => l.UserId)
                .Where(userId => !string.IsNullOrEmpty(userId))
                .Distinct()
                .ToList();

            var users = await _userService.GetUsersByIds(userIds);

            var items = logs.Select(l =>
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
                    Changes = ActivityMetadataParser.Parse(l.MetadataJson)
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
