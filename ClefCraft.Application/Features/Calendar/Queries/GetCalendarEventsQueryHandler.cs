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
            // 1. Fetch raw events within the window (date filtering now in the repository)
            var events = await _eventRepo.GetByUserIdAsync(
                request.UserId, request.RangeStart, request.RangeEnd);

            // 2. Expand recurring events
            var expanded = await _expansionService.ExpandAsync(
                events, request.RangeStart, request.RangeEnd);

            // 3. Map to DTOs and trim to window
            var dtos = expanded
                .Select(e => _mapper.Map<CalendarEventDto>(e))
                .Where(e => e.StartDate < request.RangeEnd && e.EndDate > request.RangeStart)
                .ToList();

            // 4. Enrich with board-item titles
            await _enrichmentService.EnrichAsync(dtos);

            // 5. Build AI feature vectors BEFORE recording the view signal
            //    (prevents the current-session view from leaking into the prediction features)
            var aiInputs = await _analyticsService.BuildAsync(dtos, request.UserId);

            // 6. Predict attendance
            var scores = await _predictionService.PredictAsync(aiInputs);

            // 7. Apply scores
            foreach (var dto in dtos)
            {
                if (scores.TryGetValue(dto.Id, out var score))
                    dto.AttendanceScore = score;
            }

            await _interactionService.TrackBatchAsync(
                dtos.Select(e => new Interaction("VIEW", "CalendarEvent", e.Id, 0.2)));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return dtos;
        }
    }
}