namespace ClefCraft.Application.Features.BoardItemAsks.DTOs
{
    /// <summary>What an item's asks need from the viewer, for the board card. Unlike AskSummaryDto, it depends on who's looking.</summary>
    public class AskAttentionDto
    {
        /// <summary>How many of the item's asks are the viewer's move (see AskTurn).</summary>
        public int YourMove { get; set; }
    }
}
