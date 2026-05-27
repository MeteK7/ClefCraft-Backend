using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateSeries
{
    public class UpdateSeriesOverrideAllCommand : IRequest
    {
        public string SeriesUid { get; set; }

        public string? Subject { get; set; }
        public string? Location { get; set; }
        public string? Comment { get; set; }

        public string RecurrenceRuleJson { get; set; }
    }
}
