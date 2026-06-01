using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class ReminderSchedulerService
        : IReminderSchedulerService
    {
        private readonly ICalendarReminderRepository _reminderRepo;
        private readonly INotificationQueueRepository _queueRepo;

        public ReminderSchedulerService(
            ICalendarReminderRepository reminderRepo,
            INotificationQueueRepository queueRepo)
        {
            _reminderRepo = reminderRepo;
            _queueRepo = queueRepo;
        }

        public async Task ScheduleAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken)
        {
            var reminders =
                await _reminderRepo.GetByEventIdAsync(
                    calendarEvent.Id);

            foreach (var reminder in reminders)
            {
                if (!reminder.IsEnabled)
                    continue;

                var scheduledTime =
                    calendarEvent.StartDate
                    .AddMinutes(-reminder.MinutesBeforeStart);

                if (scheduledTime <= DateTimeOffset.UtcNow)
                    continue;

                await _queueRepo.CreateAsync(
                    new NotificationQueue
                    {
                        UserId = calendarEvent.UserId,
                        CalendarEventId = calendarEvent.Id,

                        ScheduledFor = scheduledTime,

                        Message =
                            $"{calendarEvent.Subject} starts in {reminder.MinutesBeforeStart} minutes"
                    });
            }
        }
    }
}
