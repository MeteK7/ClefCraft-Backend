using ClefCraft.Application.Contracts.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface IAttendancePredictionService
    {
        Task<Dictionary<int, double?>> PredictAsync(
            List<AIEventDto> inputs);
    }
}
