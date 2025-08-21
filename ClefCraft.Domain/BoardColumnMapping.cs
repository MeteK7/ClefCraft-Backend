using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class BoardColumnMapping : BaseEntity
    {
        public int BoardId { get; set; }
        public Board Board { get; set; }

        public int BoardColumnId { get; set; }
        public BoardColumn BoardColumn { get; set; }
    }
}