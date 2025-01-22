using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class GetWorkHistoryQuery : IRequest<List<WorkHistoryDto>>
    {
        public int ItemId { get; set; }

    }
}
