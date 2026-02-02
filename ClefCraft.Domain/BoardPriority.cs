using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class BoardPriority : BaseEntity
    {
        public int? BoardId { get; set; }
        public Board? Board { get; set; }

        public int PriorityId { get; set; }
        public Priority Priority { get; set; }
    }
}
