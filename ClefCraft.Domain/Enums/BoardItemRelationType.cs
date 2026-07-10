using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain.Enums
{
    public enum BoardItemRelationType
    {
        Parent = 0,

        Blocks = 1,

        DependsOn = 2,

        Related = 3,

        Duplicate = 4,

        SplitFrom = 5
    }
}