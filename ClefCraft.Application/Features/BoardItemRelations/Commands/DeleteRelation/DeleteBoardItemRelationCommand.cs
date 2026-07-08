using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.Commands.DeleteRelation
{
    public class DeleteBoardItemRelationCommand : IRequest
    {
        public int RelationId { get; set; }
    }
}
