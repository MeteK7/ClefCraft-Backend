using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var eventDtos = _mapper.Map<List<CalendarEventDto>>(events);

            // 2️ Enrich linked board items
            var linkedIds = eventDtos
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

                foreach (var dto in eventDtos)
                {
                    if (dto.LinkedBoardItemId is int boardItemId &&
                        boardItemMap.TryGetValue(boardItemId, out var title))
                    {
                        dto.LinkedBoardItemTitle = title;
                    }
                }
            }

            // 3️ Enrich EventType info (Name + Color)
            var typeIds = eventDtos
                .Where(e => e.EventTypeId.HasValue)
                .Select(e => e.EventTypeId!.Value)
                .Distinct()
                .ToList();

            if (typeIds.Any())
            {
                // Fetch all EventTypes for the user
                var types = await _eventTypeRepository.GetByUserIdAsync(request.UserId);

                var typeMap = types
                    .Where(t => typeIds.Contains(t.Id))
                    .ToDictionary(t => t.Id);

                foreach (var dto in eventDtos)
                {
                    if (dto.EventTypeId is int typeId &&
                        typeMap.TryGetValue(typeId, out var type))
                    {
                        dto.EventTypeName = type.Name;
                        dto.EventColor = type.Color;
                    }
                }
            }

            return eventDtos;
        }
    }
}