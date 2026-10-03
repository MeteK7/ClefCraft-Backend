using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;

namespace ClefCraft.Application.Features.Calendar
{
    public static class CalendarMapper
    {
        /// <summary>
        /// Occurrence fields, attendance score and reminder minutes are filled by the handlers.
        /// </summary>
        public static CalendarEventDto ToDto(CalendarEvent calendarEvent)
        {
            return new CalendarEventDto
            {
                Id = calendarEvent.Id,
                BaseEventId = calendarEvent.BaseEventId,
                SeriesUid = calendarEvent.SeriesUid,
                OwnerUserId = calendarEvent.UserId,
                Subject = calendarEvent.Subject,
                Location = calendarEvent.Location,
                StartDate = calendarEvent.StartDate,
                EndDate = calendarEvent.EndDate,
                AllDayEvent = calendarEvent.AllDayEvent,
                EventTypeId = calendarEvent.EventTypeId,
                EventTypeName = calendarEvent.EventType?.Name,
                EventColor = calendarEvent.EventType?.Color,
                Importance = calendarEvent.Importance,
                Comment = calendarEvent.Comment,
                IsRecurring = calendarEvent.IsRecurring,
                RecurrenceRuleJson = calendarEvent.RecurrenceRuleJson,
                TimeZoneId = calendarEvent.TimeZoneId,
                LinkedBoardItemId = calendarEvent.LinkedBoardItemId,
                LinkedBoardItemTitle = calendarEvent.LinkedBoardItem?.Title
            };
        }

        /// <summary>
        /// The linked item title, attendance score and reminder minutes are filled by the handler.
        /// </summary>
        public static CalendarEventDto ToDto(CalendarEventInstanceDto instance)
        {
            return new CalendarEventDto
            {
                Id = instance.Id,
                BaseEventId = instance.BaseEventId,
                SeriesUid = instance.SeriesUid,
                OccurrenceKey = instance.OccurrenceKey,
                OccurrenceDate = instance.OccurrenceDate,
                OwnerUserId = instance.OwnerUserId,
                Subject = instance.Subject,
                Location = instance.Location,
                StartDate = instance.StartDate,
                EndDate = instance.EndDate,
                AllDayEvent = instance.AllDayEvent,
                EventTypeId = instance.EventTypeId,
                EventTypeName = instance.EventTypeName,
                EventColor = instance.EventColor,
                Importance = instance.Importance,
                Comment = instance.Comment,
                IsRecurring = instance.IsRecurring,
                RecurrenceRuleJson = instance.RecurrenceRuleJson,
                TimeZoneId = instance.TimeZoneId,
                LinkedBoardItemId = instance.LinkedBoardItemId
            };
        }

        public static CalendarEventAttachmentDto ToDto(CalendarEventAttachment attachment)
        {
            return new CalendarEventAttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                StoredFilePath = attachment.StoredFilePath,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                UploadedAt = attachment.UploadedAt,
                UploadedBy = attachment.UploadedBy
            };
        }

        public static EventTypeDto ToDto(EventType eventType)
        {
            return new EventTypeDto
            {
                Id = eventType.Id,
                Name = eventType.Name,
                Color = eventType.Color
            };
        }
    }
}
