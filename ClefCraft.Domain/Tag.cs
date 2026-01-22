using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class Tag : BaseEntity
    {
        public string Name { get; set; }
        public ICollection<BoardTag> BoardTags { get; set; } = new List<BoardTag>();
        public ICollection<BoardItemTag> BoardItemTags { get; set; } = new List<BoardItemTag>();
    }
}
