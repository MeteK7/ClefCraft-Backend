using ClefCraft.Application.Features.Calendar.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.AI
{
    public interface IAITrainingService
    {
        Task TrainFromEventsAsync(List<CalendarEventDto> events, string userId);
    }
}
