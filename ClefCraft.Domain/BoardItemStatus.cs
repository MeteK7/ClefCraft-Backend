using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class BoardItemStatus:BaseEntity
    {
        public int BoardItemId { get; set; }
        public BoardItem BoardItem { get; set; }
        public int StatusId { get; set; }
        public Status Status { get; set; }
    }
}
