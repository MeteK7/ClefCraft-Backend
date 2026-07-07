using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain.Enums
{
    public enum BoardItemRelationType
    {
        Parent = 1,

        Blocks = 2,

        DependsOn = 3,

        Related = 4,

        Duplicate = 5,

        SplitFrom = 6
    }
}