using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    public static class BoardItemAskMapper
    {
        /// <summary>
        /// Recipient, requester, completer and reply author names come from fullNames (user id to
        /// full name); replies are only ever by the requester or recipient, so UserIdsOf covers them.
        /// What the viewer may do (AllowedActions, NextNudgeAt, CanReply) is worked out from who they
        /// are, their roles, whether the item is Done and the request's single clock reading.
        /// </summary>
        public static BoardItemAskDto ToDto(
            BoardItemAsk ask,
            Domain.BoardItem item,
            IReadOnlyDictionary<string, string> fullNames,
            bool recipientIsBoardMember,
            string viewerId,
            AskActorRoles viewerRoles,
            bool itemIsDone,
            DateTime utcNow,
            IEnumerable<BoardItemAskReply> replies)
        {
            var ownReplies = replies.Where(r => r.BoardItemAskId == ask.Id).OrderBy(r => r.Id).ToList();

            return new BoardItemAskDto
            {
                Id = ask.Id,
                BoardItemId = ask.BoardItemId,
                BoardItemTitle = item.Title,
                BoardId = item.BoardId,
                BoardTitle = item.Board?.Title,
                RequestedByUserId = ask.RequestedByUserId,
                RequestedByFullName = FullNameOf(ask.RequestedByUserId, fullNames),
                RecipientUserId = ask.RecipientUserId,
                RecipientFullName = FullNameOf(ask.RecipientUserId, fullNames),
                RecipientIsBoardMember = recipientIsBoardMember,
                Note = ask.Note,
                DueDate = ask.DueDate,
                IsRequired = ask.IsRequired,
                ExpectsAnswer = ask.ExpectsAnswer,
                IsAnswered = AskReplies.IsAnswered(ask, ownReplies),
                State = ask.State,
                ResponseNote = ask.ResponseNote,
                RespondedAt = ask.RespondedAt,
                CompletedAt = ask.CompletedAt,
                CompletedByUserId = ask.CompletedByUserId,
                CompletedByFullName = ask.CompletedByUserId != null ? FullNameOf(ask.CompletedByUserId, fullNames) : null,
                CancelReason = ask.CancelReason,
                DateCreated = ask.DateCreated ?? default,
                LastNudgedAt = ask.LastNudgedAt,
                NextNudgeAt = AskTransitions.NextNudgeAt(ask, viewerRoles, itemIsDone, utcNow),
                Revision = ask.Revision,
                IsOpen = AskTransitions.IsOpen(ask.State),
                IsUnmetRequirement = AskTransitions.IsUnmetRequirement(ask),
                AllowedActions = AskTransitions.AllowedActions(ask, viewerRoles, itemIsDone, utcNow),
                YourMove = AskTurn.ReasonFor(ask, viewerId, itemIsDone),
                CanReply = AskReplies.CanReply(ask, viewerId),
                Replies = ownReplies.Select(r => new AskReplyDto
                {
                    Id = r.Id,
                    AuthorUserId = r.AuthorUserId,
                    AuthorFullName = FullNameOf(r.AuthorUserId, fullNames),
                    Body = r.Body,
                    DateCreated = r.DateCreated ?? default,
                    DateEdited = r.DateEdited,
                    IsMine = r.AuthorUserId == viewerId
                }).ToList(),
                AnswerReplyId = ask.AnswerReplyId
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

        /// <summary>What an item's asks need from the viewer: how many are their move.</summary>
        public static AskAttentionDto ToAttention(IEnumerable<BoardItemAsk> asks, string viewerId, bool itemIsDone) =>
            new() { YourMove = asks.Count(a => AskTurn.ReasonFor(a, viewerId, itemIsDone) != null) };

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
