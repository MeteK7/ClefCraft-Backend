using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class BoardTask : BaseEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int BoardColumnId { get; set; }
        public BoardColumn BoardColumn { get; set; }
    }
}
