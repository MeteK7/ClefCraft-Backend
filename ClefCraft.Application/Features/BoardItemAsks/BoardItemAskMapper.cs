using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    public static class BoardItemAskMapper
    {
        /// <summary>Recipient, requester and completer names come from fullNames (user id to full name).</summary>
        public static BoardItemAskDto ToDto(
            BoardItemAsk ask,
            Domain.BoardItem item,
            IReadOnlyDictionary<string, string> fullNames,
            bool recipientIsBoardMember,
            List<BoardItemAskAction> allowedActions)
        {
            return new BoardItemAskDto
            {
                Id = ask.Id,
                BoardItemId = ask.BoardItemId,
                BoardItemTitle = item.Title,
                BoardId = item.BoardId,
                RequestedByUserId = ask.RequestedByUserId,
                RequestedByFullName = FullNameOf(ask.RequestedByUserId, fullNames),
                RecipientUserId = ask.RecipientUserId,
                RecipientFullName = FullNameOf(ask.RecipientUserId, fullNames),
                RecipientIsBoardMember = recipientIsBoardMember,
                Note = ask.Note,
                DueDate = ask.DueDate,
                IsRequired = ask.IsRequired,
                State = ask.State,
                ResponseNote = ask.ResponseNote,
                RespondedAt = ask.RespondedAt,
                CompletedAt = ask.CompletedAt,
                CompletedByUserId = ask.CompletedByUserId,
                CompletedByFullName = ask.CompletedByUserId != null ? FullNameOf(ask.CompletedByUserId, fullNames) : null,
                CancelReason = ask.CancelReason,
                DateCreated = ask.DateCreated ?? default,
                LastNudgedAt = ask.LastNudgedAt,
                Revision = ask.Revision,
                IsOpen = AskTransitions.IsOpen(ask.State),
                IsUnmetRequirement = AskTransitions.IsUnmetRequirement(ask),
                AllowedActions = allowedActions
            };
        }

        public static AskSummaryDto ToSummary(IEnumerable<BoardItemAsk> asks, IReadOnlyDictionary<string, string> fullNames)
        {
            var list = asks.ToList();
            var open = list.Where(a => AskTransitions.IsOpen(a.State)).ToList();

            return new AskSummaryDto
            {
                OpenCount = open.Count,
                UnmetRequiredCount = list.Count(AskTransitions.IsUnmetRequirement),
                CompletedCount = list.Count(a => a.State == BoardItemAskState.Completed),
                OpenAsks = open.Select(a => new AskSummaryItemDto
                {
                    AskId = a.Id,
                    RecipientUserId = a.RecipientUserId,
                    RecipientFullName = FullNameOf(a.RecipientUserId, fullNames),
                    State = a.State,
                    IsRequired = a.IsRequired,
                    DueDate = a.DueDate,
                    DateCreated = a.DateCreated ?? default
                }).ToList()
            };
        }

        /// <summary>Every user an ask names: requester, recipient and, once done, who completed it.</summary>
        public static IEnumerable<string> UserIdsOf(BoardItemAsk ask)
        {
            yield return ask.RequestedByUserId;
            yield return ask.RecipientUserId;

            if (!string.IsNullOrEmpty(ask.CompletedByUserId))
                yield return ask.CompletedByUserId;
        }

        // "Unknown user" when the account no longer exists, as the history does for assignees.
        private static string FullNameOf(string userId, IReadOnlyDictionary<string, string> fullNames) =>
            fullNames.TryGetValue(userId, out var name) ? name : "Unknown user";
    }
}
