using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class EventEnrichmentService : IEventEnrichmentService
    {
        private readonly IBoardItemRepository _boardRepo;
        private readonly IBoardMemberRepository _boardMemberRepo;

        public EventEnrichmentService(IBoardItemRepository boardRepo, IBoardMemberRepository boardMemberRepo)
        {
            _boardRepo = boardRepo;
            _boardMemberRepo = boardMemberRepo;
        }

        public async Task EnrichAsync(List<CalendarEventDto> events, string userId)
        {
            var ids = events
                .Where(e => e.LinkedBoardItemId.HasValue)
                .Select(e => e.LinkedBoardItemId!.Value)
                .Distinct()
                .ToList();

            if (!ids.Any()) return;

            // A LinkedBoardItemId can point at a board the caller isn't a member of (either a
            // pre-existing row, or a case the create-time check upstream doesn't cover) - only
            // attach titles for items on boards the caller can actually see.
            var memberBoardIds = await _boardMemberRepo.GetMemberBoardIdsAsync(userId);
            var items = await _boardRepo.GetByIdsAsync(ids);
            var map = items
                .Where(x => memberBoardIds.Contains(x.BoardId))
                .ToDictionary(x => x.Id, x => x.Title);

            foreach (var e in events)
            {
                if (e.LinkedBoardItemId is int id && map.TryGetValue(id, out var title))
                    e.LinkedBoardItemTitle = title;
            }
        }
    }
}