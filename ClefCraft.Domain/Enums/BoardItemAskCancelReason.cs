namespace ClefCraft.Domain.Enums
{
    public enum BoardItemAskCancelReason
    {
        /// <summary>An ask manager withdrew it, or reassigned it to someone else.</summary>
        Withdrawn = 0,

        /// <summary>The item moved to Done while the ask was still open.</summary>
        ItemCompleted = 1,

        /// <summary>The recipient was removed from the item's board.</summary>
        RecipientRemoved = 2
    }
}
