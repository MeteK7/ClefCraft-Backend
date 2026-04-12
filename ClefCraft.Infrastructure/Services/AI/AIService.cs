using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Features.Calendar.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Infrastructure.Services.AI
{
    /// <summary>
    /// HTTP client wrapper for the Python AI microservice.
    /// Kept in Services/AI (not Services/Calendar) because it is a
    /// generic transport layer that is not calendar-specific.
    /// </summary>
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;

        public AIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Single-event convenience method — delegates to the batch path (DRY).
        public async Task<double> PredictAttendanceAsync(AIEventDto ev)
        {
            var results = await PredictBatchAsync(new List<AIEventDto> { ev });
            return results.FirstOrDefault();
        }

        public async Task<List<double>> PredictBatchAsync(List<AIEventDto> events)
        {
            var payload = events.Select(ev => new
            {
                ev.UserId,
                ev.EventId,

                // Temporal
                ev.StartDate,
                ev.EndDate,
                ev.DurationMinutes,
                ev.HourOfDay,
                ev.DayOfWeek,
                ev.IsRecurring,

                // Declared
                ev.Importance,

                // Behavioral signals
                ev.RescheduleCount,
                ev.AvgDaysRescheduled,
                ev.EditCount,
                ev.ViewSignalValue,

                // Task context
                ev.HasLinkedTask,
                ev.LinkedTaskReopenCount,
                ev.LinkedTaskStatusChanges,
                ev.LinkedTaskCompletionRate
            }).ToList();

            var response = await _httpClient.PostAsJsonAsync("/predict", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new AIServiceException(
                    $"AI prediction failed [{response.StatusCode}]: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();

            // Guard: if the service returns fewer results than inputs, pad with 0.
            var predictions = result?.Predictions ?? new List<double>();
            while (predictions.Count < events.Count)
                predictions.Add(0);

            return predictions;
        }
    }
}
