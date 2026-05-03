using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateSingleOccurrence
{
    public class UpdateSingleOccurrenceCommand : IRequest
    {
        public int EventId { get; set; }
        public DateTimeOffset OccurrenceDate { get; set; }
        public string? Subject { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        public bool? IsCancelled { get; set; }
    }
}
