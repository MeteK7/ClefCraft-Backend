using ClefCraft.Application.Common.Models;
using MediatR;

namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity
{
    public class GetActivityLogForEntityQuery : IRequest<PagedResult<ActivityLogEntryDto>>
    {
        public string EntityType { get; set; } = default!;
        public int EntityId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
