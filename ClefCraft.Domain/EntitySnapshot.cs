using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class EntitySnapshot : BaseEntity
    {
        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }
        public string SnapshotJson { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
