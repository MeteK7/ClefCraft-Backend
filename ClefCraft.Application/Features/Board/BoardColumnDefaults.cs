namespace ClefCraft.Application.Features.Board
{
    /// <summary>
    /// The columns a newly created board starts with, in lane order. Each board gets its own
    /// BoardColumn rows (linked through BoardColumnMapping), so they can later be changed per board.
    /// The BackfillDefaultBoardColumns migration holds its own fixed copy of these titles on purpose:
    /// changing this list only affects boards created afterwards.
    /// </summary>
    public static class BoardColumnDefaults
    {
        public static readonly IReadOnlyList<string> Titles = new[]
        {
            "Backlog",
            "To Do",
            "In Progress",
            "In Review",
            "Done"
        };
    }
}
