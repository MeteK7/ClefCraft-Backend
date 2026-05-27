using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.SplitSeriesFromOccurrence
{
    public class SplitSeriesFromOccurrenceCommand : IRequest
    {
        public int SegmentId { get; set; }
        public DateTimeOffset SplitDate { get; set; }

        public string? Subject { get; set; }
        public string? Location { get; set; }
        public string? Comment { get; set; }
        public string? RecurrenceRuleJson { get; set; }
    }
}
