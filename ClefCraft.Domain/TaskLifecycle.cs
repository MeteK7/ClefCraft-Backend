using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class TaskLifecycle : BaseEntity
    {
        public int BoardItemId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? FirstWorkedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int ReopenCount { get; set; }
        public int StatusChangeCount { get; set; }
        public int AssigneeChangeCount { get; set; }
    }
}
