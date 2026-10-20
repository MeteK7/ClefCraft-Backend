using System.Text.RegularExpressions;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using FluentValidation;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    // Replies on asks: who may post, what a reply may hold, and when an ask counts as answered.
    // A reply changes nothing about the ask itself (state, revision, allowed actions), so it is
    // allowed even while the item is Done.
    public static class AskReplies
    {
        public const string AnswerRequiredCode = "ASK_ANSWER_REQUIRED";
        public const string AnswerRequiredMessage = "This ask expects an answer. Reply with the answer before marking it done.";
        public const string CancelledMessage = "This ask was withdrawn; it can't be replied to.";

        private static readonly Regex Whitespace = new(@"\s+");

        /// <summary>Only the two people the ask is between: its requester and its recipient.</summary>
        public static bool IsParticipant(BoardItemAsk ask, string userId) =>
            userId == ask.RequestedByUserId || userId == ask.RecipientUserId;

        public static bool CanReply(BoardItemAsk ask, string userId) =>
            IsParticipant(ask, userId) && ask.State != BoardItemAskState.Cancelled;

        /// <summary>Forbidden for anyone but the requester and recipient; a 400 on a cancelled ask.</summary>
        public static void EnsureCanReply(BoardItemAsk ask, string userId)
        {
            if (!IsParticipant(ask, userId))
                throw new ForbiddenAccessException();

            if (ask.State == BoardItemAskState.Cancelled)
                throw new BadRequestException(CancelledMessage);
        }

        /// <summary>
        /// The recipient has replied since the ask was last reopened (or ever, if it never was). A
        /// reply that isn't saved yet has no DateCreated and counts: it is being posted now.
        /// </summary>
        public static bool IsAnswered(BoardItemAsk ask, IEnumerable<BoardItemAskReply> replies)
        {
            var since = ask.LastReopenedAt ?? DateTime.MinValue;

            return replies.Any(r =>
                r.BoardItemAskId == ask.Id
                && r.AuthorUserId == ask.RecipientUserId
                && (r.DateCreated ?? DateTime.MaxValue) > since);
        }

        /// <summary>A reply's text for a notification: whitespace collapsed, cut at the excerpt length.</summary>
        public static string Excerpt(string body)
        {
            var text = Whitespace.Replace(body, " ").Trim();
            var max = BoardItemAskLimits.ReplyExcerptLength;

            return text.Length > max ? text[..max].TrimEnd() + "…" : text;
        }

        /// <summary>A reply body as posted: trimmed, not empty, within the limit.</summary>
        public static IRuleBuilderOptions<T, string?> ValidReplyBody<T>(this IRuleBuilder<T, string?> rule) =>
            rule
                .Must(body => !string.IsNullOrWhiteSpace(body)).WithMessage("Write a reply")
                .Must(body => body == null || body.Trim().Length <= BoardItemAskLimits.ReplyMaxLength)
                .WithMessage($"A reply can be at most {BoardItemAskLimits.ReplyMaxLength} characters");
    }
}
