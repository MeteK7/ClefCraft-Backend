using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Tag.Queries.GetTags
{
    public class GetTagsQuery:IRequest<List<TagDto>>
    {
    }
}
