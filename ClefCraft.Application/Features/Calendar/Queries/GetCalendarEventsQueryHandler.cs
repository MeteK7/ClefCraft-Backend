using AutoMapper;
using ClefCraft.Application.Common.Helpers;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using MediatR;
using System.Text.Json;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class GetCalendarEventsQueryHandler : IRequestHandler<GetCalendarEventsQuery, List<CalendarEventDto>>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IMapper _mapper;

        public GetCalendarEventsQueryHandler(
            ICalendarEventRepository calendarEventRepository,
            IBoardItemRepository boardItemRepository,
            IEventTypeRepository eventTypeRepository,
            IMapper mapper)
        {
            _calendarEventRepository = calendarEventRepository;
            _boardItemRepository = boardItemRepository;
            _eventTypeRepository = eventTypeRepository;
            _mapper = mapper;
        }

        public async Task<List<CalendarEventDto>> Handle(
            GetCalendarEventsQuery request,
            CancellationToken cancellationToken)
        {
            // 1️ Get all events for the user
            var events = await _calendarEventRepository
                .GetByUserIdAsync(request.UserId);

            // 2️ Prepare list for expanded events
            var expandedEvents = new List<CalendarEventDto>();

            foreach (var e in events)
            {
                // Non-recurring → just map
                if (!e.IsRecurring || string.IsNullOrEmpty(e.RecurrenceRuleJson))
                {
                    expandedEvents.Add(_mapper.Map<CalendarEventDto>(e));
                    continue;
                }

                // Recurring → parse rule and expand
                RecurrenceRule? rule = null;
                try
                {
                    rule = JsonSerializer.Deserialize<RecurrenceRule>(e.RecurrenceRuleJson);
                }
                catch
                {
                    // fallback: skip invalid recurrence rules
                    expandedEvents.Add(_mapper.Map<CalendarEventDto>(e));
                    continue;
                }

                if (rule != null)
                {
                    var occurrences = RecurrenceHelper.ExpandEvent(e, rule, request.RangeStart, request.RangeEnd);
                    foreach (var occurrence in occurrences)
                    {
                        // Preserve EventTypeId and LinkedBoardItemId for later enrichment
                        occurrence.EventTypeId = e.EventTypeId;
                        occurrence.LinkedBoardItemId = e.LinkedBoardItemId;
                        occurrence.IsRecurring = true;
                        expandedEvents.Add(occurrence);
                    }
                }
            }

            // 3️ Enrich linked board items
            var linkedIds = expandedEvents
                .Where(e => e.LinkedBoardItemId.HasValue)
                .Select(e => e.LinkedBoardItemId!.Value)
                .Distinct()
                .ToList();

            if (linkedIds.Any())
            {
                var boardItems = await _boardItemRepository
                    .GetByIdsAsync(linkedIds);

                var boardItemMap = boardItems.ToDictionary(
                    bi => bi.Id,
                    bi => bi.Title
                );

                foreach (var dto in expandedEvents)
                {
                    if (dto.LinkedBoardItemId is int boardItemId &&
                        boardItemMap.TryGetValue(boardItemId, out var title))
                    {
                        dto.LinkedBoardItemTitle = title;
                    }
                }
            }

            // 4️ Enrich EventType info (Name + Color)
            var typeIds = expandedEvents
                .Where(e => e.EventTypeId.HasValue)
                .Select(e => e.EventTypeId!.Value)
                .Distinct()
                .ToList();

            if (typeIds.Any())
            {
                var types = await _eventTypeRepository.GetByUserIdAsync(request.UserId);
                var typeMap = types
                    .Where(t => typeIds.Contains(t.Id))
                    .ToDictionary(t => t.Id);

                foreach (var dto in expandedEvents)
                {
                    if (dto.EventTypeId is int typeId &&
                        typeMap.TryGetValue(typeId, out var type))
                    {
                        dto.EventTypeName = type.Name;
                        dto.EventColor = type.Color;
                    }
                }
            }

            return expandedEvents;
        }
    }
}