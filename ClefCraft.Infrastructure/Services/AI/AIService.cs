using System.Net.Http.Json;
using ClefCraft.Application.Contracts.AI;

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
            var payload = new[] { MapToPayload(ev) };

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
            var payload = events.Select(MapToPayload).ToList();

            var response = await _httpClient.PostAsJsonAsync("/predict", payload);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI ERROR: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();
            return result?.Predictions ?? new List<double>();
        }

        // Centralised mapping — avoids duplication between single and batch
        private static object MapToPayload(AIEventDto ev) => new
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
        };
    }

    internal class PredictionResponse
    {
        public List<double> Predictions { get; set; } = new();
    }
}