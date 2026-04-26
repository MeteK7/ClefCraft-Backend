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
            IMapper mapper, IUnitOfWork unitOfWork)
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
            // 1. Fetch raw events for this user
            var events = await _eventRepo.GetByUserIdAsync(request.UserId);

            // 2. Expand recurring events within the requested window
            var expanded = await _expansionService.ExpandAsync(
                events, request.RangeStart, request.RangeEnd);

            // 3. Map to DTOs and filter to the requested range
            var dtos = expanded
                .Select(e => _mapper.Map<CalendarEventDto>(e))
                // FIX: discard occurrences that fall outside the window
                .Where(e => e.StartDate < request.RangeEnd && e.EndDate > request.RangeStart)
                .ToList();

            // 4. Track views in batch
            await _interactionService.TrackBatchAsync(
                dtos.Select(e => new Interaction("VIEW", "CalendarEvent", e.Id, 0.2)));

            // 5. Enrich with board-item titles
            await _enrichmentService.EnrichAsync(dtos);

            // 6. Build AI feature vectors
            var aiInputs = await _analyticsService.BuildAsync(dtos, request.UserId);

            // 7. Predict attendance
            var scores = await _predictionService.PredictAsync(aiInputs);

            await _unitOfWork.SaveChangesAsync();

            // 8. Apply scores
            foreach (var dto in dtos)
            {
                if (scores.TryGetValue(dto.Id, out var score))
                    dto.AttendanceScore = score;
            }

            return dtos;
        }
    }
}