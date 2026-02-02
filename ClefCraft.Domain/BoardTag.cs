using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class BoardTag : BaseEntity
    {
        public int BoardId { get; set; }
        public Board Board { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
