using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItem;
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
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;

        public GetTagsHandler(
            ITagRepository tagRepository,
            IBoardAccessService boardAccessService,
            IUserService userService)
        {
            _tagRepository = tagRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
        }

        public async Task<List<TagDto>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
        {
            await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId, _userService.UserId);

            var tags = await _tagRepository
                .GetTagsByBoardIdAsync(request.BoardId);

            return tags.Select(BoardItemMapper.ToDto).ToList();
        }
    }
}
