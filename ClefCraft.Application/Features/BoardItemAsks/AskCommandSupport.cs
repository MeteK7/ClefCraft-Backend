using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    /// <summary>An ask's item as the ask commands need it: tracked, with its board owner and whether it is Done.</summary>
    public sealed record AskItemContext(Domain.BoardItem Item, string? BoardOwnerId, bool ItemIsDone)
    {
        public AskActorRoles RolesFor(BoardItemAsk ask, string callerId) =>
            AskActorRoles.For(ask, callerId, Item.AssigneeId, BoardOwnerId);
    }

    // Steps every ask command shares: loading, recipient rules, saving and the response DTO.
    public static class AskCommandSupport
    {
        public const string ConflictMessage = "This ask or its item changed while you were saving; reload and try again.";

        /// <summary>The ask, tracked. Throws NotFoundException when it doesn't exist.</summary>
        public static async Task<BoardItemAsk> LoadAskAsync(int askId, IBoardItemAskRepository askRepository) =>
            await askRepository.GetByIdAsync(askId)
                ?? throw new NotFoundException(nameof(BoardItemAsk), askId);

        /// <summary>
        /// Checks the caller is a member of the item's board (NotFound / Forbidden as for any item),
        /// then loads the item tracked, so an AskVersion increment is saved as a conditional update.
        /// </summary>
        public static async Task<AskItemContext> LoadItemAsync(
            int boardItemId,
            string callerId,
            IBoardAccessService boardAccessService,
            IBoardItemRepository boardItemRepository,
            IBoardRepository boardRepository,
            IStatusRepository statusRepository)
        {
            await boardAccessService.EnsureBoardItemOwnedByUserAsync(boardItemId, callerId);

            var item = await boardItemRepository.GetBoardItemById(boardItemId)
                ?? throw new NotFoundException(nameof(Domain.BoardItem), boardItemId);

            var board = await boardRepository.GetByIdReadOnlyAsync(item.BoardId);
            var completedStatusIds = await CompletedStatus.GetIdsAsync(statusRepository);

            return new AskItemContext(item, board?.OwnerUserId, CompletedStatus.IsCompleted(item, completedStatusIds));
        }

        /// <summary>
        /// Who a new ask may go to: a current member of the board, not the caller, and not the
        /// item's current assignee (who is already responsible for it). Checked only when an ask
        /// is created, so an existing recipient who later becomes the assignee keeps their asks.
        /// </summary>
        public static async Task EnsureValidRecipientAsync(
            string recipientUserId,
            string callerId,
            Domain.BoardItem item,
            IBoardMemberRepository boardMemberRepository)
        {
            if (recipientUserId == callerId)
                throw new BadRequestException("You can't ask yourself.");

            if (!string.IsNullOrEmpty(item.AssigneeId) && recipientUserId == item.AssigneeId)
                throw new BadRequestException("The item's assignee is already responsible for it; ask someone else.");

            if (!await boardMemberRepository.IsMemberAsync(item.BoardId, recipientUserId))
                throw new BadRequestException("The person you ask must be a member of this board.");
        }

        /// <summary>Saves, turning a concurrency conflict into a 400: nothing from the save was written.</summary>
        public static async Task SaveAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                throw new BadRequestException(ConflictMessage);
            }
        }

        public static async Task<BoardItemAskDto> ToDtoAsync(
            BoardItemAsk ask,
            AskItemContext context,
            string callerId,
            IUserService userService,
            IBoardMemberRepository boardMemberRepository)
        {
            var users = await userService.GetUsersByIds(BoardItemAskMapper.UserIdsOf(ask).Distinct().ToList());
            var fullNames = users.ToDictionary(u => u.Id, u => $"{u.Firstname} {u.Lastname}");
            var recipientIsMember = await boardMemberRepository.IsMemberAsync(context.Item.BoardId, ask.RecipientUserId);
            var allowed = AskTransitions.AllowedActions(ask, context.RolesFor(ask, callerId), context.ItemIsDone, DateTime.UtcNow);

            return BoardItemAskMapper.ToDto(ask, context.Item, fullNames, recipientIsMember, allowed);
        }
    }
}
