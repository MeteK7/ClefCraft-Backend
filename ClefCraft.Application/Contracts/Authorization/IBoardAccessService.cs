namespace ClefCraft.Application.Contracts.Authorization
{
    /// <summary>
    /// Resource-level ownership checks for the Board domain. Boards are
    /// single-owner (Board.OwnerUserId), so ownership always resolves back
    /// to one user id.
    /// </summary>
    public interface IBoardAccessService
    {
        /// <summary>
        /// Throws NotFoundException if the board doesn't exist, or
        /// ForbiddenAccessException if userId isn't its owner.
        /// </summary>
        Task EnsureBoardOwnedByUserAsync(int boardId, string userId);

        /// <summary>
        /// Throws NotFoundException if the board item doesn't exist, or
        /// ForbiddenAccessException if userId doesn't own its parent board.
        /// </summary>
        Task EnsureBoardItemOwnedByUserAsync(int boardItemId, string userId);
    }
}
