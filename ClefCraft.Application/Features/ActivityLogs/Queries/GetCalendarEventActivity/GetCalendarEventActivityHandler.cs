using ClefCraft.Application.Common.Models;
using ClefCraft.Application.Contracts.ActivityLogs;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetCalendarEventActivity
{
    public class GetCalendarEventActivityHandler : IRequestHandler<GetCalendarEventActivityQuery, PagedResult<CalendarActivityLogEntryDto>>
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly ICalendarEventSegmentRepository _segmentRepository;
        private readonly ICalendarEventExceptionRepository _exceptionRepository;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;

        public GetCalendarEventActivityHandler(
            IActivityLogRepository activityLogRepository,
            ICalendarEventSegmentRepository segmentRepository,
            ICalendarEventExceptionRepository exceptionRepository,
            ICalendarAccessService calendarAccessService,
            IUserService userService)
        {
            _activityLogRepository = activityLogRepository;
            _segmentRepository = segmentRepository;
            _exceptionRepository = exceptionRepository;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
        }

        public async Task<PagedResult<CalendarActivityLogEntryDto>> Handle(GetCalendarEventActivityQuery request, CancellationToken cancellationToken)
        {
            var validator = new GetCalendarEventActivityValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid calendar activity request", validationResult);

            await _calendarAccessService.EnsureEventOwnedByUserAsync(request.EventId, _userService.UserId);

            if (!string.IsNullOrWhiteSpace(request.SeriesUid))
            {
                // The caller could own EventId but pass an arbitrary SeriesUid belonging to
                // someone else's series — check it independently rather than assuming the two
                // always agree.
                await _calendarAccessService.EnsureSeriesOwnedByUserAsync(request.SeriesUid, _userService.UserId);
            }

            var criteria = new List<(string EntityType, IEnumerable<int> EntityIds)>
            {
                ("CalendarEvent", new[] { request.EventId })
            };

            var segmentsById = new Dictionary<int, CalendarEventSegment>();
            var exceptionsById = new Dictionary<int, CalendarEventException>();

            if (!string.IsNullOrWhiteSpace(request.SeriesUid))
            {
                var segments = await _segmentRepository.GetBySeriesUidAsync(request.SeriesUid);
                if (segments.Count > 0)
                {
                    segmentsById = segments.ToDictionary(s => s.Id);
                    criteria.Add(("CalendarEventSegment", segmentsById.Keys));
                }

                var exceptions = await _exceptionRepository.GetBySeriesUid(request.SeriesUid);
                if (exceptions.Count > 0)
                {
                    exceptionsById = exceptions.ToDictionary(e => e.Id);
                    criteria.Add(("CalendarEventException", exceptionsById.Keys));
                }
            }

            // Pagination happens at the database level (see ActivityLogRepository.GetMergedPagedAsync)
            // across all merged sources, rather than fetching every source unpaged and paging in memory.
            var skip = (request.PageNumber - 1) * request.PageSize;
            var (page, totalCount) = await _activityLogRepository.GetMergedPagedAsync(criteria, skip, request.PageSize);

            var userIds = page
                .Select(l => l.UserId)
                .Where(userId => !string.IsNullOrEmpty(userId))
                .Distinct()
                .ToList();

            var users = await _userService.GetUsersByIds(userIds);

            var items = page.Select(l =>
            {
                var user = users.FirstOrDefault(u => u.Id == l.UserId);

                var scope = l.EntityType switch
                {
                    "CalendarEventSegment" => "Segment",
                    "CalendarEventException" => "Exception",
                    _ => "Event"
                };

                DateTimeOffset? effectiveFrom = null;
                DateTimeOffset? effectiveTo = null;
                DateTimeOffset? occurrenceDate = null;

                if (scope == "Segment" && segmentsById.TryGetValue(l.EntityId, out var segment))
                {
                    effectiveFrom = segment.EffectiveFrom;
                    effectiveTo = segment.EffectiveTo;
                }
                else if (scope == "Exception" && exceptionsById.TryGetValue(l.EntityId, out var exception))
                {
                    occurrenceDate = exception.OccurrenceDate;
                }

                return new CalendarActivityLogEntryDto
                {
                    Id = l.Id,
                    Scope = scope,
                    ActionType = l.ActionType,
                    Timestamp = l.Timestamp,
                    ActorUserId = l.UserId,
                    ActorFullName = user != null ? $"{user.Firstname} {user.Lastname}" : "Unknown",
                    Changes = ActivityMetadataParser.Parse(l.MetadataJson),
                    EffectiveFrom = effectiveFrom,
                    EffectiveTo = effectiveTo,
                    OccurrenceDate = occurrenceDate
                };
            }).ToList();

            return new PagedResult<CalendarActivityLogEntryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
