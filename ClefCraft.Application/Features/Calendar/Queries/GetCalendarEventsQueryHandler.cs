using AutoMapper;
using ClefCraft.Application.Common.Helpers;
using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Text.Json;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class GetCalendarEventsQueryHandler : IRequestHandler<GetCalendarEventsQuery, List<CalendarEventDto>>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly ICalendarEventExceptionRepository _calendarEventExceptionRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IAIService _aiService;
        private readonly IMapper _mapper;
        private readonly IUserInteractionService _interactionService;
        private readonly IAIDataRepository _aiDataRepository;

        public GetCalendarEventsQueryHandler(
            ICalendarEventRepository calendarEventRepository,
            ICalendarEventExceptionRepository calendarEventExceptionRepository,
            IBoardItemRepository boardItemRepository,
            IEventTypeRepository eventTypeRepository,
            IAIService aiService,
            IMapper mapper,
            IUserInteractionService interactionService,
            IAIDataRepository aiDataRepository)
        {
            _calendarEventRepository = calendarEventRepository;
            _calendarEventExceptionRepository = calendarEventExceptionRepository;
            _boardItemRepository = boardItemRepository;
            _eventTypeRepository = eventTypeRepository;
            _aiService = aiService;
            _mapper = mapper;
            _interactionService = interactionService;
            _aiDataRepository = aiDataRepository;
        }

        public async Task<List<CalendarEventDto>> Handle(
            GetCalendarEventsQuery request,
            CancellationToken cancellationToken)
        {
            var events = await _calendarEventRepository.GetByUserIdAsync(request.UserId);

            var expandedEvents = new List<CalendarEventDto>();
            var eventIds = events.Select(e => e.Id).ToList();

            var exceptions = await _calendarEventExceptionRepository.GetByEventIdsAsync(eventIds);

            foreach (var e in events)
            {
                if (!e.IsRecurring || string.IsNullOrEmpty(e.RecurrenceRuleJson))
                {
                    expandedEvents.Add(_mapper.Map<CalendarEventDto>(e));
                    continue;
                }

                RecurrenceRule? rule = null;
                try
                {
                    rule = JsonSerializer.Deserialize<RecurrenceRule>(e.RecurrenceRuleJson);
                }
                catch
                {
                    expandedEvents.Add(_mapper.Map<CalendarEventDto>(e));
                    continue;
                }

                if (rule != null)
                {
                    var occurrences = RecurrenceHelper.ExpandEvent(
                        e, rule, exceptions, request.RangeStart, request.RangeEnd);

                    foreach (var occurrence in occurrences)
                    {
                        occurrence.EventTypeId = e.EventTypeId;
                        occurrence.LinkedBoardItemId = e.LinkedBoardItemId;
                        occurrence.IsRecurring = true;
                        expandedEvents.Add(occurrence);
                    }
                }
            }

            // 🔥 VIEW tracking
            foreach (var e in expandedEvents)
            {
                await _interactionService.TrackAsync("VIEW", "CalendarEvent", e.Id, 0.2);
            }

            // 🔥 Board enrichment
            var linkedIds = expandedEvents
                .Where(e => e.LinkedBoardItemId.HasValue)
                .Select(e => e.LinkedBoardItemId!.Value)
                .Distinct()
                .ToList();

            if (linkedIds.Any())
            {
                var boardItems = await _boardItemRepository
                    .GetByIdsAsync(linkedIds);

                var boardMap = boardItems.ToDictionary(
                    b => b.Id,
                    b => b.Title
                );

                foreach (var dto in expandedEvents)
                {
                    if (dto.LinkedBoardItemId is int id &&
                        boardMap.TryGetValue(id, out var title))
                    {
                        dto.LinkedBoardItemTitle = title;
                    }
                }
            }

            // 🔥 AI DATA
            var logs = await _aiDataRepository.GetEventLogs(expandedEvents.Select(e => e.Id).ToList());
            var signals = await _aiDataRepository.GetEventSignals(expandedEvents.Select(e => e.Id).ToList());
            var lifecycles = await _aiDataRepository.GetTaskLifecycles(linkedIds);

            var aiInputs = expandedEvents.Select(dto =>
            {
                var eventLogs = logs.Where(l => l.EntityId == dto.Id).ToList();
                var eventSignals = signals.Where(s => s.EntityId == dto.Id).ToList();

                var reschedules = eventLogs.Where(l => l.ActionType == "EVENT_RESCHEDULED").ToList();

                var avgShift = reschedules.Any()
                    ? reschedules.Select(l =>
                    {
                        var meta = JsonSerializer.Deserialize<JsonElement>(l.MetadataJson ?? "{}");
                        return meta.TryGetProperty("DaysShifted", out var v) ? v.GetDouble() : 0;
                    }).Average()
                    : 0;

                var lifecycle = dto.LinkedBoardItemId.HasValue
                    ? lifecycles.FirstOrDefault(l => l.BoardItemId == dto.LinkedBoardItemId)
                    : null;

                return new AIEventDto
                {
                    UserId = request.UserId,
                    EventId = dto.Id,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    DurationMinutes = (dto.EndDate - dto.StartDate).TotalMinutes,
                    HourOfDay = dto.StartDate.Hour,
                    DayOfWeek = (int)dto.StartDate.DayOfWeek,

                    Importance = dto.Importance,
                    IsRecurring = dto.IsRecurring,

                    RescheduleCount = reschedules.Count,
                    AvgDaysRescheduled = avgShift,
                    EditCount = eventLogs.Count(l => l.ActionType == "UPDATED"),
                    ViewSignalValue = eventSignals.Where(s => s.SignalType == "VIEW").Sum(s => s.Value),

                    HasLinkedTask = dto.LinkedBoardItemId.HasValue,
                    LinkedTaskReopenCount = lifecycle?.ReopenCount,
                    LinkedTaskStatusChanges = lifecycle?.StatusChangeCount,
                    LinkedTaskCompletionRate = lifecycle?.CompletedAt != null ? 1 : 0
                };
            }).ToList();

            var predictions = await _aiService.PredictBatchAsync(aiInputs);

            for (int i = 0; i < expandedEvents.Count; i++)
                expandedEvents[i].AttendanceScore = predictions[i];

            return expandedEvents;
        }
    }
}