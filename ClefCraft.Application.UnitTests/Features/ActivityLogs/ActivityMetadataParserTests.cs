using ClefCraft.Application.Features.ActivityLogs;
using Shouldly;
using System.Linq;

namespace ClefCraft.Application.UnitTests.Features.ActivityLogs
{
    public class ActivityMetadataParserTests
    {
        [Fact]
        public void Parse_NullMetadata_ReturnsEmptyList()
        {
            var result = ActivityMetadataParser.Parse(null);

            result.ShouldBeEmpty();
        }

        [Fact]
        public void Parse_EmptyStringMetadata_ReturnsEmptyList()
        {
            var result = ActivityMetadataParser.Parse(string.Empty);

            result.ShouldBeEmpty();
        }

        [Fact]
        public void Parse_UpdatedShapedMetadata_ReturnsOldAndNewValues()
        {
            var json = "{\"StatusId\":{\"Old\":2,\"New\":3},\"Title\":{\"Old\":\"Fix login bug\",\"New\":\"Fix login bug (urgent)\"}}";

            var result = ActivityMetadataParser.Parse(json);

            result.Count.ShouldBe(2);

            var statusChange = result.Single(c => c.FieldName == "StatusId");
            statusChange.OldValue.ShouldBe("2");
            statusChange.NewValue.ShouldBe("3");

            var titleChange = result.Single(c => c.FieldName == "Title");
            titleChange.OldValue.ShouldBe("Fix login bug");
            titleChange.NewValue.ShouldBe("Fix login bug (urgent)");
        }

        [Fact]
        public void Parse_CustomSemanticMetadata_ReturnsFlatAnnotations()
        {
            // Shape produced by IActivityLogger.LogAsync calls like EVENT_RESCHEDULED, which don't
            // use the Old/New diff shape the automatic path produces.
            var json = "{\"PreviousStart\":\"2026-08-01T10:00:00Z\",\"NewStart\":\"2026-08-02T10:00:00Z\",\"DaysShifted\":1}";

            var result = ActivityMetadataParser.Parse(json);

            result.Count.ShouldBe(3);

            var previousStart = result.Single(c => c.FieldName == "PreviousStart");
            previousStart.OldValue.ShouldBeNull();
            previousStart.NewValue.ShouldBe("2026-08-01T10:00:00Z");

            var daysShifted = result.Single(c => c.FieldName == "DaysShifted");
            daysShifted.OldValue.ShouldBeNull();
            daysShifted.NewValue.ShouldBe("1");
        }

        [Fact]
        public void Parse_MalformedJson_ReturnsEmptyListInsteadOfThrowing()
        {
            var result = ActivityMetadataParser.Parse("{not valid json");

            result.ShouldBeEmpty();
        }
    }
}
