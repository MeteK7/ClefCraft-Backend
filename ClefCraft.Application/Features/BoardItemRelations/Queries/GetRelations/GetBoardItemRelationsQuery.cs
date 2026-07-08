using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.Queries.GetRelations
{
    public class GetBoardItemRelationsQuery
        : IRequest<RelationshipHubDto>
    {
        public int ItemId { get; set; }

        public GetBoardItemRelationsQuery(int itemId)
        {
            ItemId = itemId;
        }
    }
}
