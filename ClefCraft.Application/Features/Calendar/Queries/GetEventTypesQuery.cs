using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class GetEventTypesQuery : IRequest<List<EventTypeDto>>
    {
        public string UserId { get; set; }
    }
}
