using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class RecurrenceSeries : BaseEntity
    {
        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public string SeriesUid { get; set; } = Guid.NewGuid().ToString();

        public ICollection<CalendarEventSegment> Segments { get; set; } = new List<CalendarEventSegment>();
    }
}