using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Status.Queries.GetStatuses
{
    public class GetStatusesQuery : IRequest<List<StatusDto>>
    {
        public int BoardId { get; set; }
    }
}
