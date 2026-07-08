using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.Commands.CreateRelation
{
    public class CreateBoardItemRelationCommand : IRequest<int>
    {
        public int SourceBoardItemId { get; set; }

        public int TargetBoardItemId { get; set; }

        public int RelationType { get; set; }
    }
}
