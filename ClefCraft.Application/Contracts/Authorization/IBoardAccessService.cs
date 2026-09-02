namespace ClefCraft.Application.Contracts.Authorization
{
    /// <summary>
    /// Resource-level access checks for the Board domain. Boards are
    /// team-based: any user with a BoardMember row for a board has full
    /// read/write access to it and its items, regardless of assignment.
    /// Board.OwnerUserId is creator metadata only (who made the board) — it
    /// is NOT the access gate. Use EnsureUserIsBoardOwnerAsync separately
    /// for the few actions that are deliberately owner-only (e.g. managing
    /// membership) rather than open to every member.
    /// </summary>
    public interface IBoardAccessService
    {
        /// <summary>
        /// Throws NotFoundException if the board doesn't exist, or
        /// ForbiddenAccessException if userId isn't a member of it.
        /// </summary>
        Task EnsureBoardOwnedByUserAsync(int boardId, string userId);

        /// <summary>
        /// Throws NotFoundException if the board item doesn't exist, or
        /// ForbiddenAccessException if userId isn't a member of its parent board.
        /// </summary>
        Task EnsureBoardItemOwnedByUserAsync(int boardItemId, string userId);

        /// <summary>
        /// Throws NotFoundException if the board doesn't exist, or
        /// ForbiddenAccessException if userId is not specifically its
        /// OwnerUserId (stricter than membership — for owner-only actions).
        /// </summary>
        Task EnsureUserIsBoardOwnerAsync(int boardId, string userId);
    }
}
