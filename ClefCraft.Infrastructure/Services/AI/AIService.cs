using ClefCraft.Application.Contracts.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace ClefCraft.Infrastructure.Services.AI
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;

        // Hard ceiling per prediction call; tune to your SLA
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        public AIService(HttpClient httpClient, ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<double> PredictAttendanceAsync(AIEventDto ev)
        {
            var results = await PredictBatchAsync(new List<AIEventDto> { ev });
            return results.FirstOrDefault();
        }

        public async Task<List<double>> PredictBatchAsync(List<AIEventDto> events)
        {
            if (!events.Any())
                return new List<double>();

            var payload = events.Select(MapToPayload).ToList();

            using var cts = new CancellationTokenSource(RequestTimeout);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync("/predict", payload, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                _logger.LogWarning("AI prediction timed out after {Timeout}s for {Count} events.",
                    RequestTimeout.TotalSeconds, events.Count);
                throw new AIPredictionException("Prediction service timed out.", isTransient: true);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "AI prediction network error.");
                throw new AIPredictionException("Prediction service unavailable.", isTransient: true, inner: ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI prediction failed. Status={Status} Body={Body}",
                    response.StatusCode, body);
                throw new AIPredictionException(
                    $"Prediction service returned {response.StatusCode}.", isTransient: false);
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();

            if (result?.Predictions == null || result.Predictions.Count != events.Count)
            {
                _logger.LogError(
                    "AI response count mismatch. Sent={Sent}, Received={Received}",
                    events.Count, result?.Predictions?.Count ?? 0);
                throw new AIPredictionException("Prediction count mismatch.", isTransient: false);
            }

            return result.Predictions;
        }

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

    public class AIPredictionException : Exception
    {
        public bool IsTransient { get; }

        public AIPredictionException(string message, bool isTransient, Exception? inner = null)
            : base(message, inner)
        {
            IsTransient = isTransient;
        }
    }
}