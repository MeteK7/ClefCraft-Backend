using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class BoardItemPriority:BaseEntity
    {
        public int BoardItemId { get; set; }
        public BoardItem BoardItem { get; set; }
        public int PriorityId { get; set; }
        public Priority Priority { get; set; }
    }
}
