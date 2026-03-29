using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class RecurrenceRule
    {
        public string Frequency { get; set; } = "WEEKLY";
        public int Interval { get; set; } = 1;
        public List<int>? DaysOfWeek { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int? Count { get; set; }
    }
}
