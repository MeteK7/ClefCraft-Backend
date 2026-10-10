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
        public string Name { get; set; } = null!;
        public string Color { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public List<CalendarEvent> Events { get; set; } = null!;
    }
}
