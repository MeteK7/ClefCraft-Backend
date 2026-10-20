using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Domain;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    // Item-level history entries for asks. They are written under the ask's *item*
    // (EntityType "BoardItem"), so they appear on the item's History tab; the automatic audit rows
    // SaveChangesAsync writes for BoardItemAsk itself are keyed by the ask and never shown there.
    // Each entry is added to the context before the save that makes the change, so the change and
    // its history are committed together. The actor is always the current user (ActivityLogger).
    public static class AskHistory
    {
        public const string EntityType = "BoardItem";

        public const string Created = "ASK_CREATED";
        public const string Changed = "ASK_CHANGED";
        public const string Accepted = "ASK_ACCEPTED";
        public const string Declined = "ASK_DECLINED";
        public const string Completed = "ASK_COMPLETED";
        public const string Reopened = "ASK_REOPENED";
        public const string Cancelled = "ASK_CANCELLED";
        public const string Nudged = "ASK_NUDGED";
        public const string RequirementSet = "ASK_REQUIREMENT_SET";
        public const string RequirementWaived = "ASK_REQUIREMENT_WAIVED";
        public const string RequirementMoved = "ASK_REQUIREMENT_MOVED";
        public const string RequirementsOverridden = "ASK_REQUIREMENTS_OVERRIDDEN";
        public const string AnswerExpected = "ASK_ANSWER_EXPECTED";
        public const string AnswerNotExpected = "ASK_ANSWER_NOT_EXPECTED";

        // Metadata keys. User-id keys are resolved to names by GetActivityLogForEntityHandler.
        public const string AskIdKey = "AskId";
        public const string NoteKey = "Note";
        public const string RecipientUserIdKey = "RecipientUserId";
        public const string CompletedByUserIdKey = "CompletedByUserId";
        public const string ReasonKey = "Reason";
        public const string ToRecipientUserIdKey = "ToRecipientUserId";
        public const string AskIdsKey = "AskIds";
        public const string ExpectsAnswerKey = "ExpectsAnswer";

        /// <summary>The metadata keys whose values are user ids.</summary>
        public static readonly IReadOnlySet<string> UserIdKeys =
            new HashSet<string> { RecipientUserIdKey, CompletedByUserIdKey, ToRecipientUserIdKey };

        /// <summary>
        /// Logs an entry about one ask: its id (left out for a new ask, whose id only exists once
        /// the save that also writes this entry has run), note and recipient, plus any extras.
        /// </summary>
        public static Task LogAsync(
            IActivityLogger activityLogger,
            string actionType,
            BoardItemAsk ask,
            IReadOnlyDictionary<string, object?>? extra = null)
        {
            var metadata = new Dictionary<string, object>();

            if (ask.Id != 0)
                metadata[AskIdKey] = ask.Id;

            metadata[NoteKey] = ask.Note;
            metadata[RecipientUserIdKey] = ask.RecipientUserId;

            if (extra != null)
            {
                foreach (var (key, value) in extra)
                {
                    if (value != null)
                        metadata[key] = value;
                }
            }

            return activityLogger.LogAsync(EntityType, ask.BoardItemId, actionType, metadata);
        }

        /// <summary>
        /// Logs that the requirement on <paramref name="from"/> moved to a new ask for
        /// <paramref name="toRecipientUserId"/> (Ask again or Reassign).
        /// </summary>
        public static Task LogRequirementMovedAsync(IActivityLogger activityLogger, BoardItemAsk from, string toRecipientUserId) =>
            LogAsync(activityLogger, RequirementMoved, from,
                new Dictionary<string, object?> { [ToRecipientUserIdKey] = toRecipientUserId });

        /// <summary>Logs, on the item, exactly which unmet requirements the caller completed it past.</summary>
        public static Task LogRequirementsOverriddenAsync(IActivityLogger activityLogger, int boardItemId, IEnumerable<int> askIds) =>
            activityLogger.LogAsync(EntityType, boardItemId, RequirementsOverridden,
                new Dictionary<string, object> { [AskIdsKey] = askIds.ToArray() });
    }
}
