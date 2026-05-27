using ClefCraft.Domain.Common;
using ClefCraft.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class CalendarEventSegment : BaseEntity
    {
        public int RecurrenceSeriesId { get; set; }
        public RecurrenceSeries RecurrenceSeries { get; set; }

        public DateTimeOffset EffectiveFrom { get; set; }
        public DateTimeOffset? EffectiveTo { get; set; }

        public string Subject { get; set; }
        public string? Location { get; set; }
        public string? Comment { get; set; }

        public bool IsRecurring { get; set; }
        public string? RecurrenceRuleJson { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public ImportanceLevel Importance { get; set; }
        public int? EventTypeId { get; set; }
    }
}
