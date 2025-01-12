using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class CalendarEvent : BaseEntity
    {
        public string Subject { get; set; }
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AllDayEvent { get; set; }
        public string Importance { get; set; }
        //public string Label { get; set; }
        public string? Comment { get; set; }
        public string UserId { get; set; }
        public int? LinkedBoardItemId { get; set; }
        public virtual BoardItem LinkedBoardItem { get; set; }
        public List<CalendarEventHistory> History { get; set; }
    }

}
