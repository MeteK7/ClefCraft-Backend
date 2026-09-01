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

        private record MergedEntry(
            ActivityLog Log,
            string Scope,
            DateTimeOffset? EffectiveFrom,
            DateTimeOffset? EffectiveTo,
            DateTimeOffset? OccurrenceDate);

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

            var merged = new List<MergedEntry>();

            var eventLogs = await _activityLogRepository.GetByEntityTypeAndIdsAsync("CalendarEvent", new[] { request.EventId });
            merged.AddRange(eventLogs.Select(l => new MergedEntry(l, "Event", null, null, null)));

            if (!string.IsNullOrWhiteSpace(request.SeriesUid))
            {
                var segments = await _segmentRepository.GetBySeriesUidAsync(request.SeriesUid);
                if (segments.Count > 0)
                {
                    var segmentsById = segments.ToDictionary(s => s.Id);
                    var segmentLogs = await _activityLogRepository.GetByEntityTypeAndIdsAsync("CalendarEventSegment", segmentsById.Keys);

                    merged.AddRange(segmentLogs.Select(l =>
                    {
                        var segment = segmentsById[l.EntityId];
                        return new MergedEntry(l, "Segment", segment.EffectiveFrom, segment.EffectiveTo, null);
                    }));
                }

                var exceptions = await _exceptionRepository.GetBySeriesUid(request.SeriesUid);
                if (exceptions.Count > 0)
                {
                    var exceptionsById = exceptions.ToDictionary(e => e.Id);
                    var exceptionLogs = await _activityLogRepository.GetByEntityTypeAndIdsAsync("CalendarEventException", exceptionsById.Keys);

                    merged.AddRange(exceptionLogs.Select(l =>
                    {
                        var exception = exceptionsById[l.EntityId];
                        return new MergedEntry(l, "Exception", null, null, exception.OccurrenceDate);
                    }));
                }
            }

            var ordered = merged.OrderByDescending(m => m.Log.Timestamp).ToList();
            var totalCount = ordered.Count;

            var page = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var userIds = page
                .Select(m => m.Log.UserId)
                .Where(userId => !string.IsNullOrEmpty(userId))
                .Distinct()
                .ToList();

            var users = await _userService.GetUsersByIds(userIds);

            var items = page.Select(m =>
            {
                var user = users.FirstOrDefault(u => u.Id == m.Log.UserId);

                return new CalendarActivityLogEntryDto
                {
                    Id = m.Log.Id,
                    Scope = m.Scope,
                    ActionType = m.Log.ActionType,
                    Timestamp = m.Log.Timestamp,
                    ActorUserId = m.Log.UserId,
                    ActorFullName = user != null ? $"{user.Firstname} {user.Lastname}" : "Unknown",
                    Changes = ActivityMetadataParser.Parse(m.Log.MetadataJson),
                    EffectiveFrom = m.EffectiveFrom,
                    EffectiveTo = m.EffectiveTo,
                    OccurrenceDate = m.OccurrenceDate
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
