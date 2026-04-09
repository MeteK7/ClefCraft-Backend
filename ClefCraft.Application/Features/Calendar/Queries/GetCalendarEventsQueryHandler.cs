using AutoMapper;
using ClefCraft.Application.Common.Helpers;
using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Models.Analytics;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Text.Json;

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

        public GetCalendarEventsQueryHandler(
            ICalendarEventRepository eventRepo,
            IEventExpansionService expansionService,
            IEventEnrichmentService enrichmentService,
            IEventAnalyticsService analyticsService,
            IAttendancePredictionService predictionService,
            IUserInteractionService interactionService,
            IMapper mapper)
        {
            _eventRepo = eventRepo;
            _expansionService = expansionService;
            _enrichmentService = enrichmentService;
            _analyticsService = analyticsService;
            _predictionService = predictionService;
            _interactionService = interactionService;
            _mapper = mapper;
        }

        public async Task<List<CalendarEventDto>> Handle(
            GetCalendarEventsQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Fetch
            var events = await _eventRepo.GetByUserIdAsync(request.UserId);

            // 2. Expand recurring
            var expanded = await _expansionService.ExpandAsync(
                events, request.RangeStart, request.RangeEnd);

            // 3. Map
            var dtos = expanded.Select(e => _mapper.Map<CalendarEventDto>(e)).ToList();

            // 4. Track (BATCH ✅)
            await _interactionService.TrackBatchAsync(
                dtos.Select(e => new Interaction("VIEW", "CalendarEvent", e.Id, 0.2))
            );

            // 5. Enrich (board data)
            await _enrichmentService.EnrichAsync(dtos);

            // 6. Build AI features
            var aiInputs = await _analyticsService.BuildAsync(dtos, request.UserId);

            // 7. Predict
            var scores = await _predictionService.PredictAsync(aiInputs);

            // 8. Apply scores safely
            foreach (var dto in dtos)
            {
                if (scores.TryGetValue(dto.Id, out var score))
                    dto.AttendanceScore = score;
            }

            return dtos;
        }
    }
}