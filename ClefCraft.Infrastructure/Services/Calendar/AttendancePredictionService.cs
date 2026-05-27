using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Infrastructure.Services.AI;
using Microsoft.Extensions.Logging;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class AttendancePredictionService : IAttendancePredictionService
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AttendancePredictionService> _logger;

        public AttendancePredictionService(IAIService aiService, ILogger<AttendancePredictionService> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<Dictionary<int, double>> PredictAsync(List<AIEventDto> inputs)
        {
            if (!inputs.Any())
                return new Dictionary<int, double>();

            try
            {
                var predictions = await _aiService.PredictBatchAsync(inputs);

                return inputs
                    .Select((input, index) => new
                    {
                        input.EventId,
                        Score = predictions[index]
                    })
                    .GroupBy(x => x.EventId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Last().Score
                    );
            }
            catch (AIPredictionException ex)
            {
                // Graceful degradation: return neutral scores rather than crashing the calendar.
                _logger.LogWarning(ex, "Attendance prediction failed; falling back to neutral scores.");

                return inputs.ToDictionary(i => i.EventId, _ => -1.0); // -1 signals "unavailable" to the UI
            }
        }
    }
}