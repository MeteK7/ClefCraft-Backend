using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    /// <summary>
    /// What the user decided about the item's asks when completing it, built from what the prompt
    /// showed them (GetAsksForItem).
    /// </summary>
    public class AskClosureRequest
    {
        /// <summary>Every ask the prompt listed: the open asks plus the unmet requirements.</summary>
        public List<int> AcknowledgedAskIds { get; set; } = new();

        /// <summary>Accepted asks to mark Completed (for their recipient). The other open asks are cancelled.</summary>
        public List<int> CompleteAskIds { get; set; } = new();

        /// <summary>Exactly the unmet requirements the user chose to complete the item past.</summary>
        public List<int> OverrideAskIds { get; set; } = new();
    }

    public sealed class AskClosureResult
    {
        public static readonly AskClosureResult None = new(new List<BoardItemAsk>(), new List<BoardItemAsk>(), new List<int>());

        public AskClosureResult(IReadOnlyList<BoardItemAsk> completed, IReadOnlyList<BoardItemAsk> cancelled, IReadOnlyList<int> overriddenAskIds)
        {
            Completed = completed;
            Cancelled = cancelled;
            OverriddenAskIds = overriddenAskIds;
        }

        public IReadOnlyList<BoardItemAsk> Completed { get; }
        public IReadOnlyList<BoardItemAsk> Cancelled { get; }
        public IReadOnlyList<int> OverriddenAskIds { get; }
    }

    // The rule for completing an item that has asks. Everything is validated before any ask is
    // changed, so a rejected request leaves the asks exactly as they were.
    public static class AskClosure
    {
        public const string MissingMessage = "This item has open or required asks; review them before completing it.";
        public const string ChangedMessage = "The item's asks changed; review them again.";
        public const string DuplicateMessage = "An ask is listed more than once.";

        /// <summary>The asks the user has to review before completing the item: open ones and unmet requirements.</summary>
        public static List<BoardItemAsk> AsksToReview(IEnumerable<BoardItemAsk> itemAsks) =>
            itemAsks.Where(a => AskTransitions.IsOpen(a.State) || AskTransitions.IsUnmetRequirement(a)).ToList();

        /// <summary>
        /// Validates the request against the item's current asks and applies it: selected Accepted
        /// asks become Completed (completed by the caller, credited to their recipient), every other
        /// open ask becomes Cancelled (ItemCompleted). Terminal unmet requirements are left as they
        /// are; completing past them needs them listed in OverrideAskIds.
        /// </summary>
        /// <exception cref="BadRequestException">The request doesn't match the item's asks.</exception>
        /// <exception cref="ForbiddenAccessException">The caller may not mark a selected ask done.</exception>
        public static AskClosureResult Apply(
            IReadOnlyCollection<BoardItemAsk> itemAsks,
            AskClosureRequest? request,
            Func<BoardItemAsk, AskActorRoles> rolesFor,
            string callerId,
            DateTime utcNow)
        {
            var toReview = AsksToReview(itemAsks);
            if (toReview.Count == 0)
                return AskClosureResult.None;

            if (request == null)
                throw new BadRequestException(MissingMessage);

            var acknowledged = request.AcknowledgedAskIds ?? new List<int>();
            var complete = request.CompleteAskIds ?? new List<int>();
            var overrides = request.OverrideAskIds ?? new List<int>();

            if (HasDuplicates(acknowledged) || HasDuplicates(complete) || HasDuplicates(overrides))
                throw new BadRequestException(DuplicateMessage);

            // The prompt must have shown exactly the asks there are now: this rejects a stale
            // prompt, an ask created after it opened, and ids that aren't this item's.
            var byId = toReview.ToDictionary(a => a.Id);
            if (!byId.Keys.ToHashSet().SetEquals(acknowledged))
                throw new BadRequestException(ChangedMessage);

            foreach (var askId in complete)
            {
                var ask = byId[askId];

                if (ask.State != BoardItemAskState.Accepted)
                    throw new BadRequestException($"Ask {askId} can't be marked done: only an accepted ask can.");

                if (!AskTransitions.HasRole(BoardItemAskAction.CompleteOnBehalf, rolesFor(ask)))
                    throw new ForbiddenAccessException();
            }

            // Every requirement left unmet must be overridden by name. An ask made required after
            // the prompt opened isn't in the user's list, so it can't be completed past unseen.
            var completeIds = complete.ToHashSet();
            var unmet = toReview
                .Where(a => a.IsRequired && !completeIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToHashSet();

            if (!unmet.SetEquals(overrides))
                throw new BadRequestException(ChangedMessage);

            var completed = new List<BoardItemAsk>();
            var cancelled = new List<BoardItemAsk>();

            // Each ask gets at most one transition: the cancelled set is the open asks minus the
            // completed ones.
            foreach (var ask in toReview)
            {
                if (completeIds.Contains(ask.Id))
                {
                    ask.State = BoardItemAskState.Completed;
                    ask.CompletedAt = utcNow;
                    ask.CompletedByUserId = callerId;
                    ask.Revision++;
                    completed.Add(ask);
                }
                else if (AskTransitions.IsOpen(ask.State))
                {
                    ask.State = BoardItemAskState.Cancelled;
                    ask.CancelReason = BoardItemAskCancelReason.ItemCompleted;
                    ask.Revision++;
                    cancelled.Add(ask);
                }
            }

            return new AskClosureResult(completed, cancelled, overrides.ToList());
        }

        private static bool HasDuplicates(List<int> ids) => ids.Count != ids.Distinct().Count();
    }
}
