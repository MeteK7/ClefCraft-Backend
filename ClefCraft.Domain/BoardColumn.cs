using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClefCraft.Domain
{
    public class BoardColumn : BaseEntity
    {
        public string Title { get; set; } = null!;
        public List<BoardItem> BoardItems { get; set; } = null!;
    }
}
