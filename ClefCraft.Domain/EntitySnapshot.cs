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
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string SnapshotJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
