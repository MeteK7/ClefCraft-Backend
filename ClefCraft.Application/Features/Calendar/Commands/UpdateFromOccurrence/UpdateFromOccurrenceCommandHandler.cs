using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateFromOccurrence
{
    public class UpdateFromOccurrenceCommandHandler : IRequestHandler<UpdateFromOccurrenceCommand, bool>
    {
        private readonly ICalendarEventRepository _eventRepo;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateFromOccurrenceCommandHandler(ICalendarEventRepository eventRepo, IUnitOfWork unitOfWork)
        {
            _eventRepo = eventRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateFromOccurrenceCommand request, CancellationToken cancellationToken)
        {
            var baseEvent = await _eventRepo.GetByIdAsync(request.BaseEventId);
            if (baseEvent == null) return false;

            // 1. Cap the old recurring series rule right before this occurrence date
            if (!string.IsNullOrEmpty(baseEvent.RecurrenceRuleJson))
            {
                var originalRule = JsonSerializer.Deserialize<RecurrenceRule>(baseEvent.RecurrenceRuleJson);
                if (originalRule != null)
                {
                    originalRule.EndDate = request.OccurrenceDate.AddDays(-1);
                    baseEvent.RecurrenceRuleJson = JsonSerializer.Serialize(originalRule);
                    await _eventRepo.UpdateAsync(baseEvent);
                }
            }

            // 2. Spawn a new recurring series starting at the split date using updated form details
            var newSeriesEvent = new CalendarEvent
            {
                UserId = baseEvent.UserId,
                Subject = request.Subject,
                Comment = request.Comment,
                Location = request.Location,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsRecurring = baseEvent.IsRecurring,
                RecurrenceRuleJson = baseEvent.RecurrenceRuleJson, // Copies structure, rule has mutated
                EventTypeId = baseEvent.EventTypeId,
                Importance = baseEvent.Importance,
                AllDayEvent = baseEvent.AllDayEvent
            };

            await _eventRepo.CreateAsync(newSeriesEvent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}