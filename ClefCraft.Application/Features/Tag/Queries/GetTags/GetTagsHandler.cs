using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Tag.Queries.GetTags
{
    public class GetTagsHandler:IRequestHandler<GetTagsQuery, List<TagDto>>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public GetTagsHandler(ITagRepository tagRepository, IMapper mapper)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<List<TagDto>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
        {
            var tags=await _tagRepository.GetTags();
            return _mapper.Map<List<TagDto>>(tags);
        }
    }
}
