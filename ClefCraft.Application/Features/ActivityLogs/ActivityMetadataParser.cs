using ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ClefCraft.Application.Features.ActivityLogs
{
    // ActivityLog.MetadataJson has no single fixed shape: the automatic diffing path in
    // SaveChangesAsync writes { "PropName": { "Old": ..., "New": ... } } for UPDATED rows, while
    // manual IActivityLogger.LogAsync calls (e.g. "EVENT_RESCHEDULED", "IMPORTANCE_CHANGED") write
    // arbitrary custom-shaped JSON. This parser is deliberately generic and best-effort rather than
    // strictly typed per ActionType, so it doesn't need updating every time a new LogAsync call
    // site introduces another shape.
    public static class ActivityMetadataParser
    {
        public static List<ActivityFieldChangeDto> Parse(string? metadataJson)
        {
            var result = new List<ActivityFieldChangeDto>();

            if (string.IsNullOrWhiteSpace(metadataJson))
                return result;

            try
            {
                using var document = JsonDocument.Parse(metadataJson);

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var value = property.Value;

                    if (value.ValueKind == JsonValueKind.Object &&
                        value.TryGetProperty("Old", out var oldValue) &&
                        value.TryGetProperty("New", out var newValue))
                    {
                        result.Add(new ActivityFieldChangeDto
                        {
                            FieldName = property.Name,
                            OldValue = ElementToString(oldValue),
                            NewValue = ElementToString(newValue)
                        });
                    }
                    else
                    {
                        // Custom/semantic metadata (e.g. EVENT_RESCHEDULED's PreviousStart/NewStart)
                        // doesn't share the Old/New diff shape — treat each top-level key as a flat
                        // annotation instead of a before/after diff.
                        result.Add(new ActivityFieldChangeDto
                        {
                            FieldName = property.Name,
                            OldValue = null,
                            NewValue = ElementToString(value)
                        });
                    }
                }
            }
            catch (JsonException)
            {
                // A malformed MetadataJson row shouldn't break the whole feed for an entity —
                // fall through and return whatever was parsed so far (empty on failure at the root).
                return new List<ActivityFieldChangeDto>();
            }

            return result;
        }

        private static string? ElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                _ => element.GetRawText()
            };
        }
    }
}
