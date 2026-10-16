using ClefCraft.Application.Contracts.Persistence;

namespace ClefCraft.Application.Features.BoardItem
{
    /// <summary>
    /// The completed status, resolved by name: status ids depend on the order the statuses were
    /// created in, and every board offers its own copy of the standard statuses.
    /// </summary>
    public static class CompletedStatus
    {
        public const string Name = "Done";

        public static async Task<HashSet<int>> GetIdsAsync(IStatusRepository statusRepository) =>
            (await statusRepository.GetStatusesAsync())
                .Where(s => s.Name == Name)
                .Select(s => s.Id)
                .ToHashSet();

        public static bool IsCompleted(Domain.BoardItem item, HashSet<int> completedStatusIds) =>
            item.BoardItemStatus != null && completedStatusIds.Contains(item.BoardItemStatus.StatusId);
    }
}
