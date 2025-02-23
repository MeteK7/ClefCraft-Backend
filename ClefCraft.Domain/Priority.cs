using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class Priority : BaseEntity
    {
        public string Name { get; set; }
        public ICollection<BoardItem> BoardItems { get; set; } = new List<BoardItem>();
    }
}
