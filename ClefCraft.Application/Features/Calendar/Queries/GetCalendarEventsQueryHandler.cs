using AutoMapper;
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
        private readonly IEventExpansionService _expansionService;
        private readonly IEventEnrichmentService _enrichmentService;
        private readonly IEventAnalyticsService _analyticsService;
        private readonly IAttendancePredictionService _predictionService;
        private readonly IUserInteractionService _interactionService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public GetCalendarEventsQueryHandler(
            ICalendarEventRepository eventRepo,
            IEventExpansionService expansionService,
            IEventEnrichmentService enrichmentService,
            IEventAnalyticsService analyticsService,
            IAttendancePredictionService predictionService,
            IUserInteractionService interactionService,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _eventRepo = eventRepo;
            _expansionService = expansionService;
            _enrichmentService = enrichmentService;
            _analyticsService = analyticsService;
            _predictionService = predictionService;
            _interactionService = interactionService;
            _mapper = mapper;
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
            var expanded = await _expansionService.ExpandAsync(
                events, request.RangeStart, request.RangeEnd);

            // 3. Map to DTOs and trim to window
            //    BaseEventId is mapped by convention from CalendarEvent → CalendarEventDto
            var dtos = expanded
                .Select(e => _mapper.Map<CalendarEventDto>(e))
                .Where(e => e.StartDate < request.RangeEnd && e.EndDate > request.RangeStart)
                .ToList();

            // 4. Enrich with board-item titles
            await _enrichmentService.EnrichAsync(dtos);

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
    }
}