using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardMembers.DTOs;
using ClefCraft.Domain;
using MediatR;

namespace ClefCraft.Application.Features.BoardMembers.Commands.AddMember
{
    public class AddBoardMemberCommandHandler : IRequestHandler<AddBoardMemberCommand, BoardMemberDto>
    {
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public AddBoardMemberCommandHandler(
            IBoardMemberRepository boardMemberRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _boardMemberRepository = boardMemberRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BoardMemberDto> Handle(AddBoardMemberCommand request, CancellationToken cancellationToken)
        {
            await _boardAccessService.EnsureUserIsBoardOwnerAsync(request.BoardId, request.RequestingUserId);

            var existing = await _boardMemberRepository.GetByBoardAndUserAsync(request.BoardId, request.UserId);
            if (existing != null)
                throw new BadRequestException($"User {request.UserId} is already a member of this board.");

            var member = new BoardMember
            {
                BoardId = request.BoardId,
                UserId = request.UserId
            };

            await _boardMemberRepository.CreateAsync(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var user = await _userService.GetUser(request.UserId);

            return new BoardMemberDto
            {
                Id = member.Id,
                BoardId = member.BoardId,
                UserId = member.UserId,
                FullName = user != null ? $"{user.Firstname} {user.Lastname}" : "Unknown"
            };
        }
    }
}
