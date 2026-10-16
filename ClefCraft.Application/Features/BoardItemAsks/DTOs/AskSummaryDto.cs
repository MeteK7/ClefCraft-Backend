using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks.DTOs
{
    /// <summary>
    /// What a board card shows about an item's asks. The completion prompt doesn't use this: it
    /// loads the item's asks themselves (GetAsksForItem).
    /// </summary>
    public class AskSummaryDto
    {
        public int OpenCount { get; set; }
        public int UnmetRequiredCount { get; set; }
        public int CompletedCount { get; set; }
        public List<AskSummaryItemDto> OpenAsks { get; set; } = new();
    }

    public class AskSummaryItemDto
    {
        public int AskId { get; set; }
        public string RecipientUserId { get; set; } = null!;
        public string RecipientFullName { get; set; } = null!;
        public BoardItemAskState State { get; set; }
        public bool IsRequired { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
