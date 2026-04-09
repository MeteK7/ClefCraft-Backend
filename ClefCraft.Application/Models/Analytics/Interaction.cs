using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Models.Analytics
{
    public class Interaction
    {
        public string SignalType { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public double Value { get; set; }

        public Interaction(string signalType, string entityType, int entityId, double value = 1)
        {
            SignalType = signalType;
            EntityType = entityType;
            EntityId = entityId;
            Value = value;
        }
    }
}
