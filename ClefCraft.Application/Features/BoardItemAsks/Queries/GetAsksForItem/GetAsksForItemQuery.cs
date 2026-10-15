using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetAsksForItem
{
    /// <summary>
    /// Every ask on the item, oldest first, in any state. Includes everything the completion prompt
    /// needs: each open ask and each unmet requirement (IsOpen / IsUnmetRequirement), with its state,
    /// IsRequired, Revision and the caller's AllowedActions.
    /// </summary>
    public class GetAsksForItemQuery : IRequest<List<BoardItemAskDto>>
    {
        public int BoardItemId { get; set; }

        public GetAsksForItemQuery(int boardItemId)
        {
            BoardItemId = boardItemId;
        }
    }
}
