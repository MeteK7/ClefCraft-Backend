using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Shouldly;

namespace ClefCraft.Application.UnitTests.Mapping
{
    public class CalendarMappingTests
    {
        private static readonly DateTimeOffset Start = new(2026, 6, 15, 18, 0, 0, TimeSpan.FromHours(3));
        private static readonly DateTimeOffset End = new(2026, 6, 15, 19, 30, 0, TimeSpan.FromHours(3));

        private static CalendarEvent FullEvent() => new()
        {
            Id = 21,
            BaseEventId = 20,
            Subject = "Piano lesson",
            Location = "Studio B",
            StartDate = Start,
            EndDate = End,
            AllDayEvent = false,
            EventTypeId = 5,
            EventType = new EventType { Id = 5, Name = "Lesson", Color = "#ff8800" },
            Importance = ImportanceLevel.High,
            Comment = "Bring the etude book",
            UserId = "user-1",
            LinkedBoardItemId = 11,
            LinkedBoardItem = new BoardItem { Id = 11, Title = "Scales" },
            SeriesUid = "series-1",
            IsRecurring = true,
            RecurrenceRuleJson = "{\"Frequency\":\"WEEKLY\"}",
            TimeZoneId = "Europe/Istanbul",
        };

        // ── CalendarEvent → CalendarEventDto ────────────────────────────────────────

        [Fact]
        public void CalendarEvent_MapsAllSourceFields()
        {
            var dto = MappingUnderTest.ToCalendarEventDto(FullEvent());

            dto.Id.ShouldBe(21);
            dto.BaseEventId.ShouldBe(20);
            dto.SeriesUid.ShouldBe("series-1");
            dto.OwnerUserId.ShouldBe("user-1");
            dto.Subject.ShouldBe("Piano lesson");
            dto.Location.ShouldBe("Studio B");
            dto.StartDate.ShouldBe(Start);
            dto.EndDate.ShouldBe(End);
            dto.AllDayEvent.ShouldBeFalse();
            dto.EventTypeId.ShouldBe(5);
            dto.EventTypeName.ShouldBe("Lesson");
            dto.EventColor.ShouldBe("#ff8800");
            dto.Importance.ShouldBe(ImportanceLevel.High);
            dto.Comment.ShouldBe("Bring the etude book");
            dto.IsRecurring.ShouldBeTrue();
            dto.RecurrenceRuleJson.ShouldBe("{\"Frequency\":\"WEEKLY\"}");
            dto.TimeZoneId.ShouldBe("Europe/Istanbul");
            dto.LinkedBoardItemId.ShouldBe(11);
        }

        [Fact]
        public void CalendarEvent_TakesTheLinkedItemTitleFromTheLoadedBoardItem()
        {
            var dto = MappingUnderTest.ToCalendarEventDto(FullEvent());

            dto.LinkedBoardItemTitle.ShouldBe("Scales");
        }

        [Fact]
        public void CalendarEvent_LeavesOccurrenceScoreAndRemindersForTheHandler()
        {
            var dto = MappingUnderTest.ToCalendarEventDto(FullEvent());

            dto.OccurrenceKey.ShouldBeNull();
            dto.OccurrenceDate.ShouldBeNull();
            dto.AttendanceScore.ShouldBeNull();
            dto.ReminderMinutes.ShouldBeEmpty();
        }

        [Fact]
        public void CalendarEvent_WithoutEventTypeOrLinkedItem_HasNullNamesColorAndTitle()
        {
            var calendarEvent = FullEvent();
            calendarEvent.EventTypeId = null;
            calendarEvent.EventType = null;
            calendarEvent.LinkedBoardItemId = null;
            calendarEvent.LinkedBoardItem = null;

            var dto = MappingUnderTest.ToCalendarEventDto(calendarEvent);

            dto.EventTypeId.ShouldBeNull();
            dto.EventTypeName.ShouldBeNull();
            dto.EventColor.ShouldBeNull();
            dto.LinkedBoardItemId.ShouldBeNull();
            dto.LinkedBoardItemTitle.ShouldBeNull();
        }

        [Fact]
        public void CalendarEvent_WithLinkedItemIdButItemNotLoaded_HasNullTitle()
        {
            var calendarEvent = FullEvent();
            calendarEvent.LinkedBoardItem = null;

            var dto = MappingUnderTest.ToCalendarEventDto(calendarEvent);

            dto.LinkedBoardItemId.ShouldBe(11);
            dto.LinkedBoardItemTitle.ShouldBeNull();
        }

        // ── CalendarEventInstanceDto → CalendarEventDto ─────────────────────────────

        [Fact]
        public void Instance_MapsAllFieldsIncludingTheOccurrence()
        {
            var occurrence = new DateTimeOffset(2026, 6, 22, 18, 0, 0, TimeSpan.FromHours(3));
            var instance = new CalendarEventInstanceDto
            {
                Id = 21,
                BaseEventId = 20,
                OwnerUserId = "user-1",
                SeriesUid = "series-1",
                OccurrenceKey = "series-1:2026-06-22",
                OccurrenceDate = occurrence,
                Subject = "Piano lesson",
                Location = "Studio B",
                StartDate = Start.AddDays(7),
                EndDate = End.AddDays(7),
                AllDayEvent = false,
                EventTypeId = 5,
                EventTypeName = "Lesson",
                EventColor = "#ff8800",
                Importance = ImportanceLevel.Low,
                Comment = "Bring the etude book",
                IsRecurring = true,
                RecurrenceRuleJson = "{\"Frequency\":\"WEEKLY\"}",
                TimeZoneId = "Europe/Istanbul",
                LinkedBoardItemId = 11,
            };

            var dto = MappingUnderTest.ToCalendarEventDto(instance);

            dto.Id.ShouldBe(21);
            dto.BaseEventId.ShouldBe(20);
            dto.OwnerUserId.ShouldBe("user-1");
            dto.SeriesUid.ShouldBe("series-1");
            dto.OccurrenceKey.ShouldBe("series-1:2026-06-22");
            dto.OccurrenceDate.ShouldBe(occurrence);
            dto.Subject.ShouldBe("Piano lesson");
            dto.Location.ShouldBe("Studio B");
            dto.StartDate.ShouldBe(Start.AddDays(7));
            dto.EndDate.ShouldBe(End.AddDays(7));
            dto.AllDayEvent.ShouldBeFalse();
            dto.EventTypeId.ShouldBe(5);
            dto.EventTypeName.ShouldBe("Lesson");
            dto.EventColor.ShouldBe("#ff8800");
            dto.Importance.ShouldBe(ImportanceLevel.Low);
            dto.Comment.ShouldBe("Bring the etude book");
            dto.IsRecurring.ShouldBeTrue();
            dto.RecurrenceRuleJson.ShouldBe("{\"Frequency\":\"WEEKLY\"}");
            dto.TimeZoneId.ShouldBe("Europe/Istanbul");
            dto.LinkedBoardItemId.ShouldBe(11);

            // Filled in later by the handler (enrichment, prediction, reminders).
            dto.LinkedBoardItemTitle.ShouldBeNull();
            dto.AttendanceScore.ShouldBeNull();
            dto.ReminderMinutes.ShouldBeEmpty();
        }

        // ── CalendarEventAttachment → CalendarEventAttachmentDto ────────────────────

        [Fact]
        public void Attachment_MapsAllFields()
        {
            var uploadedAt = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

            var dto = MappingUnderTest.ToAttachmentDto(new CalendarEventAttachment
            {
                Id = 31,
                CalendarEventId = 21,
                FileName = "etude.pdf",
                StoredFilePath = "21/abc.pdf",
                FileSize = 2048,
                ContentType = "application/pdf",
                UploadedAt = uploadedAt,
                UploadedBy = "user-1",
            });

            dto.Id.ShouldBe(31);
            dto.FileName.ShouldBe("etude.pdf");
            dto.StoredFilePath.ShouldBe("21/abc.pdf");
            dto.FileSize.ShouldBe(2048);
            dto.ContentType.ShouldBe("application/pdf");
            dto.UploadedAt.ShouldBe(uploadedAt);
            dto.UploadedBy.ShouldBe("user-1");
        }

        [Fact]
        public void Attachments_MapEachInOrder()
        {
            var dtos = MappingUnderTest.ToAttachmentDtos(new[]
            {
                new CalendarEventAttachment { Id = 1, FileName = "a.pdf" },
                new CalendarEventAttachment { Id = 2, FileName = "b.png" },
            });

            dtos.Select(d => (d.Id, d.FileName)).ShouldBe(new[] { (1, "a.pdf"), (2, "b.png") });
        }

        // ── EventType → EventTypeDto ────────────────────────────────────────────────

        [Fact]
        public void EventTypes_MapIdNameAndColorInOrder()
        {
            var dtos = MappingUnderTest.ToEventTypeDtos(new[]
            {
                new EventType { Id = 1, Name = "Lesson", Color = "#ff8800", UserId = "user-1" },
                new EventType { Id = 2, Name = "Practice", Color = "#0088ff", UserId = "user-1" },
            });

            dtos.Select(d => (d.Id, d.Name, d.Color))
                .ShouldBe(new[] { (1, "Lesson", "#ff8800"), (2, "Practice", "#0088ff") });
        }
    }
}
