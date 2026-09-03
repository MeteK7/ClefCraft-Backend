using MediatR;

namespace ClefCraft.Application.Features.BoardItem.Queries.GetUserFullName
{
    public class GetUserFullNameQuery : IRequest<string>
    {
        public string TargetUserId { get; set; }

        public GetUserFullNameQuery(string targetUserId)
        {
            TargetUserId = targetUserId;
        }
    }
}
