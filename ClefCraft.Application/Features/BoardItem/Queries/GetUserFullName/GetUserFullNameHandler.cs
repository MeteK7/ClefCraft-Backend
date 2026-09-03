using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using MediatR;

namespace ClefCraft.Application.Features.BoardItem.Queries.GetUserFullName
{
    public class GetUserFullNameHandler : IRequestHandler<GetUserFullNameQuery, string>
    {
        private readonly IUserService _userService;
        private readonly IBoardMemberRepository _boardMemberRepository;

        public GetUserFullNameHandler(IUserService userService, IBoardMemberRepository boardMemberRepository)
        {
            _userService = userService;
            _boardMemberRepository = boardMemberRepository;
        }

        public async Task<string> Handle(GetUserFullNameQuery request, CancellationToken cancellationToken)
        {
            var callerId = _userService.UserId;

            // Resolving your own name is always fine; resolving someone else's requires
            // sharing at least one board with them - otherwise this is a userId enumeration
            // that discloses another user's name with no relationship to the caller at all.
            if (request.TargetUserId != callerId &&
                !await _boardMemberRepository.ShareAnyBoardAsync(callerId, request.TargetUserId))
            {
                throw new ForbiddenAccessException();
            }

            var user = await _userService.GetUser(request.TargetUserId);

            if (user == null)
                throw new NotFoundException(nameof(Models.Identity.User), request.TargetUserId);

            return $"{user.Firstname} {user.Lastname}";
        }
    }
}
