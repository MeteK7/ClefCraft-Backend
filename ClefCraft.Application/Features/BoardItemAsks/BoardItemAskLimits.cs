namespace ClefCraft.Application.Features.BoardItemAsks
{
    /// <summary>Limits for asks, shared by the validators and the column lengths.</summary>
    public static class BoardItemAskLimits
    {
        public const int NoteMaxLength = 200;
        public const int ResponseNoteMaxLength = 500;

        /// <summary>A Pending ask can be nudged at most once in this period.</summary>
        public static readonly TimeSpan NudgeInterval = TimeSpan.FromHours(24);

        /// <summary>How long a declined ask stays in its requester's "Asks you sent" list.</summary>
        public static readonly TimeSpan SentDeclinedWindow = TimeSpan.FromDays(14);
    }
}
