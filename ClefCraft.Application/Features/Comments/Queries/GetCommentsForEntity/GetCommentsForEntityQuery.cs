using ClefCraft.Application.Common.Models;
using MediatR;

namespace ClefCraft.Application.Features.Comments.Queries.GetCommentsForEntity
{
    public class GetCommentsForEntityQuery : IRequest<PagedResult<CommentDto>>
    {
        public string EntityType { get; set; } = default!;
        public int EntityId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
