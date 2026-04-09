using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class EventEnrichmentService : IEventEnrichmentService
    {
        private readonly IBoardItemRepository _boardRepo;

        public EventEnrichmentService(IBoardItemRepository boardRepo)
        {
            _boardRepo = boardRepo;
        }

        public async Task EnrichAsync(List<CalendarEventDto> events)
        {
            var ids = events
                .Where(e => e.LinkedBoardItemId.HasValue)
                .Select(e => e.LinkedBoardItemId!.Value)
                .Distinct()
                .ToList();

            if (!ids.Any()) return;

            var items = await _boardRepo.GetByIdsAsync(ids);
            var map = items.ToDictionary(x => x.Id, x => x.Title);

            foreach (var e in events)
            {
                if (e.LinkedBoardItemId is int id && map.TryGetValue(id, out var title))
                    e.LinkedBoardItemTitle = title;
            }
        }
    }
}
