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
        private readonly IMapper _mapper;

        public GetCalendarEventsQueryHandler(ICalendarEventRepository calendarEventRepository, IBoardItemRepository boardItemRepository, IMapper mapper)
        {
            _calendarEventRepository = calendarEventRepository;
            _boardItemRepository = boardItemRepository;
            _mapper = mapper;
        }

        public async Task<List<CalendarEventDto>> Handle(
            GetCalendarEventsQuery request,
            CancellationToken cancellationToken)
        {
            var events = await _calendarEventRepository
                .GetByUserIdAsync(request.UserId);

            var eventDtos = _mapper.Map<List<CalendarEventDto>>(events);

            // ✅ Collect all linked board item IDs
            var linkedIds = eventDtos
                .Where(e => e.LinkedBoardItemId.HasValue)
                .Select(e => e.LinkedBoardItemId!.Value)
                .Distinct()
                .ToList();

            if (!linkedIds.Any())
                return eventDtos;

            // ✅ Fetch board items in one go (NO N+1)
            var boardItems = await _boardItemRepository
                .GetByIdsAsync(linkedIds);

            var boardItemMap = boardItems.ToDictionary(
                bi => bi.Id,
                bi => bi.Title
            );

            // ✅ Enrich DTOs
            foreach (var dto in eventDtos)
            {
                if (dto.LinkedBoardItemId is int boardItemId &&
                    boardItemMap.TryGetValue(boardItemId, out var title))
                {
                    dto.LinkedBoardItemTitle = title;
                }
            }

            return eventDtos;
        }
    }
}
