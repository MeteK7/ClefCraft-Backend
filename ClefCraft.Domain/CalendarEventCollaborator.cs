using ClefCraft.Domain.Common;

namespace ClefCraft.Domain
{
    // A per-event, read-only access grant — the sole visibility/comment mechanism for a
    // CalendarEvent belonging to someone else. Deliberately independent of LinkedBoardItemId:
    // logging work against a shared BoardItem never implies anyone else can see the resulting
    // personal calendar record. Granted only via the event owner mentioning someone in a
    // comment (with explicit confirmation in the UI) — there is no separate "add collaborator"
    // entry point. CreatedBy/DateCreated (from BaseEntity) double as "granted by / granted at".
    public class CalendarEventCollaborator : BaseEntity
    {
        public int CalendarEventId { get; set; }
        public string UserId { get; set; }
    }
}
