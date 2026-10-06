using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Models.Analytics;
using MediatR;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class GetCalendarEventsQueryHandler
        : IRequestHandler<GetCalendarEventsQuery, List<CalendarEventDto>>
    {
        private readonly ICalendarEventRepository _eventRepo;
        private readonly IRecurringEventProjectionService _projectionService;
        private readonly IEventEnrichmentService _enrichmentService;
        private readonly IEventAnalyticsService _analyticsService;
        private readonly IAttendancePredictionService _predictionService;
        private readonly IUserInteractionService _interactionService;
        private readonly ICalendarReminderRepository _reminderRepo;
        private readonly IEventTypeRepository _eventTypeRepo;
        private readonly IUnitOfWork _unitOfWork;

        public GetCalendarEventsQueryHandler(
            ICalendarEventRepository eventRepo,
            IRecurringEventProjectionService projectionService,
            IEventEnrichmentService enrichmentService,
            IEventAnalyticsService analyticsService,
            IAttendancePredictionService predictionService,
            IUserInteractionService interactionService,
            ICalendarReminderRepository reminderRepo,
            IEventTypeRepository eventTypeRepo,
            IUnitOfWork unitOfWork)
        {
            _eventRepo = eventRepo;
            _projectionService = projectionService;
            _enrichmentService = enrichmentService;
            _analyticsService = analyticsService;
            _predictionService = predictionService;
            _interactionService = interactionService;
            _reminderRepo = reminderRepo;
            _eventTypeRepo = eventTypeRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<CalendarEventDto>> Handle(
            GetCalendarEventsQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Fetch raw events within the window
            var events = await _eventRepo.GetByUserIdAsync(
                request.UserId, request.RangeStart, request.RangeEnd);

            // 2. Expand recurring events (BaseEventId is populated here)
            var expanded = await _projectionService.ProjectAsync(
                events,
                request.RangeStart,
                request.RangeEnd);

            // 3. Map to DTOs and trim to window
            var dtos = expanded
                .Select(e => CalendarMapper.ToDto(e))
                .Where(e => e.StartDate < request.RangeEnd && e.EndDate > request.RangeStart)
                .ToList();

            // 3.1 Event type name/colour for occurrences that only carry an EventTypeId. Recurring
            //     occurrences are built from segments and single-occurrence edits, which can each
            //     change the type, so resolve by id (one query) rather than copying the root's.
            await FillMissingEventTypesAsync(dtos);

            // 3.2 Load reminders in a single query (NO N+1)
            var eventIds = dtos.Select(x => x.Id).Distinct().ToList();

            var reminders = await _reminderRepo.GetByEventIdsAsync(eventIds);

            // group into lookup: EventId → reminder minutes
            var reminderLookup = reminders
                .GroupBy(r => r.CalendarEventId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.MinutesBeforeStart).ToList()
                );

            // attach to DTOs
            foreach (var dto in dtos)
            {
                if (reminderLookup.TryGetValue(dto.Id, out var minutes))
                {
                    dto.ReminderMinutes = minutes;
                }
                else
                {
                    dto.ReminderMinutes = new List<int>();
                }
            }

            // 4. Enrich with board-item titles
            await _enrichmentService.EnrichAsync(dtos, request.UserId);

            // 5. Build AI feature vectors using BaseEventId so analytics queries
            //    hit real DB records even for expanded recurring occurrences.
            //    This runs BEFORE recording the view signal to prevent leakage.
            var uniqueDtos = dtos
            .GroupBy(x => x.BaseEventId)
            .Select(g => g.First())
            .ToList();

            var aiInputs = await _analyticsService.BuildAsync(uniqueDtos, request.UserId);

            // 6. Predict attendance
            var scores = await _predictionService.PredictAsync(aiInputs);

            // 7. Apply scores
            foreach (var dto in dtos)
            {
                if (scores.TryGetValue(dto.BaseEventId, out var score))
                    dto.AttendanceScore = score;
            }

            // 8. Track view signals using BaseEventId (real FK, not synthetic occurrence ID)
            //    Note: this is a deliberate side effect in a query handler — kept here
            //    because the view signal must be recorded atomically with the response.
            await _interactionService.TrackBatchAsync(
                dtos.Select(e => new Interaction("VIEW", "CalendarEvent", e.BaseEventId, 0.2)));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return dtos;
        }

        private async Task FillMissingEventTypesAsync(List<CalendarEventDto> dtos)
        {
            var missing = dtos
                .Where(d => d.EventTypeId.HasValue && d.EventTypeName == null)
                .ToList();
            if (missing.Count == 0)
                return;

            var ids = missing.Select(d => d.EventTypeId!.Value).Distinct().ToList();
            var types = (await _eventTypeRepo.GetByIdsAsync(ids)).ToDictionary(t => t.Id);

            foreach (var dto in missing)
            {
                if (types.TryGetValue(dto.EventTypeId!.Value, out var type))
                {
                    dto.EventTypeName = type.Name;
                    dto.EventColor = type.Color;
                }
            }
        }
    }
}