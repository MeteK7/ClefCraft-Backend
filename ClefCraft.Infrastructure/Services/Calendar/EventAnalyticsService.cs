using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System.Text.Json;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class EventAnalyticsService : IEventAnalyticsService
    {
        private readonly IAIDataRepository _repo;

        public EventAnalyticsService(IAIDataRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<AIEventDto>> BuildAsync(
            List<CalendarEventDto> events,
            string userId)
        {
            var eventIds = events.Select(e => e.Id).ToList();

            var linkedIds = events
                .Where(e => e.LinkedBoardItemId.HasValue)
                .Select(e => e.LinkedBoardItemId!.Value)
                .Distinct()
                .ToList();

            var logs = await _repo.GetEventLogs(eventIds);
            var signals = await _repo.GetEventSignals(eventIds);
            var lifecycles = linkedIds.Any()
                ? await _repo.GetTaskLifecycles(linkedIds)
                : new List<TaskLifecycle>();

            // O(1) lookups
            var logsMap = logs
                .GroupBy(x => x.EntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var signalsMap = signals
                .GroupBy(x => x.EntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var lifecycleMap = lifecycles.ToDictionary(x => x.BoardItemId);

            return events.Select(dto =>
            {
                logsMap.TryGetValue(dto.Id, out var eventLogs);
                signalsMap.TryGetValue(dto.Id, out var eventSignals);

                eventLogs ??= new List<ActivityLog>();
                eventSignals ??= new List<UserInteractionSignal>();

                var reschedules = eventLogs
                    .Where(l => l.ActionType == "EVENT_RESCHEDULED")
                    .ToList();

                var avgShift = reschedules.Any()
                    ? reschedules.Average(l =>
                    {
                        try
                        {
                            var meta = JsonSerializer.Deserialize<JsonElement>(l.MetadataJson ?? "{}");
                            return meta.TryGetProperty("DaysShifted", out var v) ? v.GetDouble() : 0;
                        }
                        catch
                        {
                            return 0;
                        }
                    })
                    : 0;

                // FIX: guard with HasValue before dictionary lookup
                TaskLifecycle? lifecycle = null;
                if (dto.LinkedBoardItemId.HasValue)
                    lifecycleMap.TryGetValue(dto.LinkedBoardItemId.Value, out lifecycle);

                return new AIEventDto
                {
                    UserId = userId,
                    EventId = dto.Id,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    DurationMinutes = (dto.EndDate - dto.StartDate).TotalMinutes,
                    HourOfDay = dto.StartDate.Hour,
                    DayOfWeek = (int)dto.StartDate.DayOfWeek,
                    Importance = dto.Importance,
                    IsRecurring = dto.IsRecurring,
                    RescheduleCount = reschedules.Count,
                    AvgDaysRescheduled = avgShift,
                    EditCount = eventLogs.Count(l => l.ActionType == "UPDATED"),
                    ViewSignalValue = eventSignals
                        .Where(s => s.SignalType == "VIEW")
                        .Sum(s => s.Value),
                    HasLinkedTask = dto.LinkedBoardItemId.HasValue,
                    LinkedTaskReopenCount = lifecycle?.ReopenCount,
                    LinkedTaskStatusChanges = lifecycle?.StatusChangeCount,
                    LinkedTaskCompletionRate = lifecycle?.CompletedAt != null ? 1.0 : 0.0
                };
            }).ToList();
        }
    }
}