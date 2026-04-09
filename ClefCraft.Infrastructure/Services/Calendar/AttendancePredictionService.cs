using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Contracts.Calendar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class AttendancePredictionService : IAttendancePredictionService
    {
        private readonly IAIService _aiService;

        public AttendancePredictionService(IAIService aiService)
        {
            _aiService = aiService;
        }

        public async Task<Dictionary<int, double>> PredictAsync(List<AIEventDto> inputs)
        {
            var predictions = await _aiService.PredictBatchAsync(inputs);

            return inputs
                .Select((input, index) => new { input.EventId, Score = predictions[index] })
                .ToDictionary(x => x.EventId, x => x.Score);
        }
    }
}
