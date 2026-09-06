using MediatR;

namespace ClefCraft.Application.Features.Comments.Queries.GetMentionableUsers
{
    public class GetMentionableUsersQuery : IRequest<List<MentionableUserDto>>
    {
        public string EntityType { get; set; } = default!;
        public int EntityId { get; set; }
    }
}
