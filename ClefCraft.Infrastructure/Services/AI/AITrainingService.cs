using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Infrastructure.Services.AI
{
    public class AITrainingService : IAITrainingService
    {
        private readonly IEventAnalyticsService _analyticsService;
        private readonly IAIDataRepository _repo;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AITrainingService> _logger;

        public AITrainingService(
            IEventAnalyticsService analyticsService,
            IAIDataRepository repo,
            HttpClient httpClient,
            ILogger<AITrainingService> logger)
        {
            _analyticsService = analyticsService;
            _repo = repo;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task TrainFromEventsAsync(List<CalendarEventDto> events, string userId)
        {
            if (!events.Any())
                return;

            var eventIds = events.Select(e => e.Id).ToList();

            // 🔹 Pull raw behavioral data
            var logs = await _repo.GetEventLogs(eventIds);
            var signals = await _repo.GetEventSignals(eventIds);

            var logsMap = logs
                .GroupBy(x => x.EntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var signalsMap = signals
                .GroupBy(x => x.EntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 🔹 Build AI features (existing pipeline)
            var aiEvents = await _analyticsService.BuildAsync(events, userId);

            // 🔹 Generate labels
            var labels = events.Select(e =>
            {
                logsMap.TryGetValue(e.Id, out var eventLogs);
                signalsMap.TryGetValue(e.Id, out var eventSignals);

                eventLogs ??= new List<ActivityLog>();
                eventSignals ??= new List<UserInteractionSignal>();

                return GenerateLabel(eventLogs, eventSignals);
            }).ToList();

            var payload = new
            {
                events = aiEvents.Select(MapToPayload),
                labels = labels
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/train", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI training failed. Status={Status}, Body={Body}",
                        response.StatusCode, body);
                    return;
                }

                _logger.LogInformation("AI model trained with {Count} samples.", events.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI training request failed.");
            }
        }

        // ---------------------------------------
        // CORE: Label generation from behavior
        // ---------------------------------------
        private static int GenerateLabel(
            List<ActivityLog> logs,
            List<UserInteractionSignal> signals)
        {
            // ❌ Strong negative: deleted
            if (logs.Any(l => l.ActionType == "DELETED"))
                return 0;

            var rescheduleCount = logs.Count(l => l.ActionType == "EVENT_RESCHEDULED");

            var viewScore = signals
                .Where(s => s.SignalType == "VIEW")
                .Sum(s => s.Value);

            var editScore = signals
                .Where(s => s.SignalType == "EDIT")
                .Sum(s => s.Value);

            var dragScore = signals
                .Where(s => s.SignalType == "DRAG_DROP")
                .Sum(s => s.Value);

            // 🔹 Engagement score
            var engagement = viewScore + (editScore * 2) + (dragScore * 0.5);

            // ❌ Heavy rescheduling → likely skipped
            if (rescheduleCount >= 3 && engagement < 1)
                return 0;

            // ❌ No engagement at all
            if (engagement <= 0)
                return 0;

            // ✅ Otherwise attended
            return 1;
        }

        private static object MapToPayload(AIEventDto ev)
        {
            return new
            {
                userId = ev.UserId,
                eventId = ev.EventId,
                startDate = ev.StartDate,
                endDate = ev.EndDate,
                durationMinutes = ev.DurationMinutes,
                hourOfDay = ev.HourOfDay,
                dayOfWeek = ev.DayOfWeek,
                importance = (int)ev.Importance,
                isRecurring = ev.IsRecurring,
                rescheduleCount = ev.RescheduleCount,
                avgDaysRescheduled = ev.AvgDaysRescheduled,
                editCount = ev.EditCount,
                viewSignalValue = ev.ViewSignalValue,
                hasLinkedTask = ev.HasLinkedTask,
                linkedTaskReopenCount = ev.LinkedTaskReopenCount,
                linkedTaskStatusChanges = ev.LinkedTaskStatusChanges,
                linkedTaskCompletionRate = ev.LinkedTaskCompletionRate
            };
        }
    }
}