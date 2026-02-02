using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class Board:BaseEntity
    {
        //public int Id { get; set; }
        public string Title { get; set; }
        public ICollection<BoardTag> BoardTags { get; set; } = new List<BoardTag>();
        public ICollection<BoardStatus> BoardStatuses { get; set; } = new List<BoardStatus>();
        public ICollection<BoardPriority> BoardPriorities { get; set; } = new List<BoardPriority>();
    }
}
