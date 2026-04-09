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
                ev.StartDate,
                ev.EndDate,
                ev.Importance,
                ev.IsRecurring
            }
        };

            var response = await _httpClient.PostAsJsonAsync(
                "http://localhost:8000/predict",
                payload
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI ERROR: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();

            return result.Predictions.First();
        }

        public async Task<List<double>> PredictBatchAsync(List<AIEventDto> events)
        {
            var payload = events.Select(ev => new
            {
                ev.UserId,
                ev.StartDate,
                ev.EndDate,
                ev.Importance,
                ev.IsRecurring
            }).ToList();

            var response = await _httpClient.PostAsJsonAsync("/predict", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI ERROR: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();

            return result.Predictions;
        }
    }


    public class PredictionResponse
    {
        public List<double> Predictions { get; set; }
    }
}
