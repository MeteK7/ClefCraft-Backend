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
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;

        public AIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<double> PredictAttendanceAsync(AIEventDto ev)
        {
            var payload = new[]
            {
        new
        {
            ev.UserId,
            ev.EventId,

            ev.StartDate,
            ev.EndDate,
            ev.DurationMinutes,
            ev.HourOfDay,
            ev.DayOfWeek,
            ev.IsRecurring,

            ev.Importance,

            ev.RescheduleCount,
            ev.AvgDaysRescheduled,
            ev.EditCount,
            ev.ViewSignalValue,

            ev.HasLinkedTask,
            ev.LinkedTaskReopenCount,
            ev.LinkedTaskStatusChanges,
            ev.LinkedTaskCompletionRate
        }
    };

            var response = await _httpClient.PostAsJsonAsync("/predict", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI ERROR: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();

            return result?.Predictions?.FirstOrDefault() ?? 0;
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
                throw new Exception($"AI ERROR: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();

            return result?.Predictions ?? new List<double>();
        }
    }


    public class PredictionResponse
    {
        public List<double> Predictions { get; set; }
    }
}
