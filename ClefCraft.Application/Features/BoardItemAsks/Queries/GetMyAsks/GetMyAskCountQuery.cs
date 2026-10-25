using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks
{
    /// <summary>
    /// How many asks one of the caller's lists holds, without loading them: the "Waiting for you"
    /// badge. Same rules and boards as GetMyAsksQuery.
    /// </summary>
    public class GetMyAskCountQuery : IRequest<int>
    {
        public AskDirection Direction { get; set; }
    }
}
