using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClefCraft.Domain
{
    public class BoardColumn : BaseEntity
    {
        public int Id { get; set; }
        public int BoardId { get; set; }
        public string Title { get; set; }
        public List<BoardItem> BoardItems { get; set; }
    }
}
