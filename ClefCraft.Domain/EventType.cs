using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class EventType : BaseEntity
    {
        public string Name { get; set; }
        public string Color { get; set; }

        public string UserId { get; set; }

        public List<CalendarEvent> Events { get; set; }
    }
}
