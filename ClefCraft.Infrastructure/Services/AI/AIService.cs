using ClefCraft.Application.Contracts.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace ClefCraft.Infrastructure.Services.AI
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;

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
                throw new AIPredictionException("Prediction service timed out.", true);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "AI prediction network error.");
                throw new AIPredictionException("Prediction service unavailable.", true, ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI prediction failed. Status={Status} Body={Body}",
                    response.StatusCode, body);
                throw new AIPredictionException(
                    $"Prediction service returned {response.StatusCode}.", false);
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();

            if (result?.Predictions == null || result.Predictions.Count != events.Count)
            {
                _logger.LogError(
                    "AI response count mismatch. Sent={Sent}, Received={Received}",
                    events.Count, result?.Predictions?.Count ?? 0);
                throw new AIPredictionException("Prediction count mismatch.", false);
            }

            return result.Predictions;
        }

        private static AIPredictionRequest MapToPayload(AIEventDto ev)
        {
            return new AIPredictionRequest
            {
                UserId = ev.UserId,
                EventId = ev.EventId,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate,
                DurationMinutes = ev.DurationMinutes,
                HourOfDay = ev.HourOfDay,
                DayOfWeek = ev.DayOfWeek,
                IsRecurring = ev.IsRecurring,
                Importance = (int)ev.Importance,
                RescheduleCount = ev.RescheduleCount,
                AvgDaysRescheduled = ev.AvgDaysRescheduled,
                EditCount = ev.EditCount,
                ViewSignalValue = ev.ViewSignalValue,
                HasLinkedTask = ev.HasLinkedTask,
                LinkedTaskReopenCount = ev.LinkedTaskReopenCount,
                LinkedTaskStatusChanges = ev.LinkedTaskStatusChanges,
                LinkedTaskCompletionRate = ev.LinkedTaskCompletionRate
            };
        }
    }

    public class AIPredictionRequest
    {
        public string UserId { get; set; }
        public int EventId { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public double DurationMinutes { get; set; }
        public int HourOfDay { get; set; }
        public int DayOfWeek { get; set; }

        public bool IsRecurring { get; set; }
        public int Importance { get; set; }

        public int RescheduleCount { get; set; }
        public double AvgDaysRescheduled { get; set; }
        public int EditCount { get; set; }
        public double ViewSignalValue { get; set; }

        public bool HasLinkedTask { get; set; }
        public int LinkedTaskReopenCount { get; set; }
        public int LinkedTaskStatusChanges { get; set; }
        public double LinkedTaskCompletionRate { get; set; }
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