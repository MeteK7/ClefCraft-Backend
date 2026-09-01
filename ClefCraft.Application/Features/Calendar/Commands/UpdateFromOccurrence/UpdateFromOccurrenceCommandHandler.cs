using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateFromOccurrence
{
    public class UpdateFromOccurrenceCommandHandler
        : IRequestHandler<UpdateFromOccurrenceCommand>
    {
        private readonly IRecurrenceSeriesRepository _seriesRepo;
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly ICalendarEventExceptionRepository _exceptionRepo;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _uow;

        public UpdateFromOccurrenceCommandHandler(
            IRecurrenceSeriesRepository seriesRepo,
            ICalendarEventSegmentRepository segmentRepo,
            ICalendarEventExceptionRepository exceptionRepo,
            ICalendarAccessService calendarAccessService,
            IUserService userService,
            IUnitOfWork uow)
        {
            _seriesRepo = seriesRepo;
            _segmentRepo = segmentRepo;
            _exceptionRepo = exceptionRepo;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
            _uow = uow;
        }

        public async Task<Unit> Handle(
            UpdateFromOccurrenceCommand request,
            CancellationToken cancellationToken)
        {
            await _calendarAccessService.EnsureSeriesOwnedByUserAsync(request.SeriesUid, _userService.UserId);

            var series = await _seriesRepo.GetBySeriesUidAsync(request.SeriesUid);

            if (series == null)
                throw new NotFoundException(nameof(RecurrenceSeries), request.SeriesUid);

            // FIX: Look for the segment that specifically contains the requested date 
            // by evaluating existing constraints tightly, ordered by descending start times.
            var activeSegment = await _segmentRepo.GetActiveSegmentAsync(
                series.Id,
                request.OccurrenceDate);

            if (activeSegment == null)
                throw new NotFoundException(nameof(CalendarEventSegment), request.OccurrenceDate.ToString());

            // 1. Cap the old segment strictly BEFORE the targeted split occurrence date
            activeSegment.EffectiveTo = request.OccurrenceDate;
            await _segmentRepo.UpdateAsync(activeSegment);

            var originalDuration = activeSegment.EndDate - activeSegment.StartDate;
            var occurrenceStart = request.StartDate ?? request.OccurrenceDate;
            var occurrenceEnd = request.EndDate ?? (occurrenceStart + originalDuration);

            // FIX: If a segment already exists with EXACTLY the same EffectiveFrom date, 
            // we are re-updating a split boundary rather than making a new slice.
            // To prevent infinite stack accumulation, check your current series segments.
            var existingSegmentAtDate = series.Segments.FirstOrDefault(s => s.EffectiveFrom == request.OccurrenceDate);

            if (existingSegmentAtDate != null)
            {
                // Update the existing split in place instead of piling on an overlapping segment
                existingSegmentAtDate.Subject = request.Subject ?? existingSegmentAtDate.Subject;
                existingSegmentAtDate.Location = request.Location ?? existingSegmentAtDate.Location;
                existingSegmentAtDate.Comment = request.Comment ?? existingSegmentAtDate.Comment;
                existingSegmentAtDate.StartDate = occurrenceStart;
                existingSegmentAtDate.EndDate = occurrenceEnd;
                existingSegmentAtDate.RecurrenceRuleJson = request.RecurrenceRuleJson ?? existingSegmentAtDate.RecurrenceRuleJson;
                existingSegmentAtDate.Importance = request.Importance ?? existingSegmentAtDate.Importance;
                existingSegmentAtDate.EventTypeId = request.EventTypeId ?? existingSegmentAtDate.EventTypeId;

                await _segmentRepo.UpdateAsync(existingSegmentAtDate);
            }
            else
            {
                // Create a new segment trailing cleanly out into the open future timeline
                var newSegment = new CalendarEventSegment
                {
                    RecurrenceSeriesId = series.Id,
                    EffectiveFrom = request.OccurrenceDate,
                    EffectiveTo = null, // Open-ended future progression

                    Subject = request.Subject ?? activeSegment.Subject,
                    Location = request.Location ?? activeSegment.Location,
                    Comment = request.Comment ?? activeSegment.Comment,

                    StartDate = occurrenceStart,
                    EndDate = occurrenceEnd,

                    IsRecurring = activeSegment.IsRecurring,
                    RecurrenceRuleJson = request.RecurrenceRuleJson ?? activeSegment.RecurrenceRuleJson,

                    Importance = request.Importance ?? activeSegment.Importance,
                    EventTypeId = request.EventTypeId ?? activeSegment.EventTypeId
                };

                await _segmentRepo.CreateAsync(newSegment);
            }

            // 3. Clear downstream overrides that are no longer valid against the new rule configuration
            await _exceptionRepo.DeleteFromDateAsync(
                request.SeriesUid,
                request.OccurrenceDate);

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}