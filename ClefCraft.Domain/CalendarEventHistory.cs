using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class CalendarEventHistory : BaseEntity
    {
        public int CalendarEventId { get; set; }
        public CalendarEvent CalendarEvent { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeDescription { get; set; }
        public string ChangedBy { get; set; }
    }

}
