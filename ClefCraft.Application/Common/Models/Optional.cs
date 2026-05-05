using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Common.Models
{
    public class Optional<T>
    {
        public bool HasValue { get; set; }
        public T? Value { get; set; }
    }
}
