using AutoMapper;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Board.Queries.GetBoards;
using ClefCraft.Domain;
using MediatR;

namespace ClefCraft.Application.Features.Board.Commands.CreateBoard
{
    public class CreateBoardCommandHandler : IRequestHandler<CreateBoardCommand, BoardDto>
    {
        private readonly IBoardRepository _boardRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBoardCommandHandler(
            IBoardRepository boardRepository,
            IBoardMemberRepository boardMemberRepository,
            IMapper mapper,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _boardRepository = boardRepository;
            _boardMemberRepository = boardMemberRepository;
            _mapper = mapper;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BoardDto> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new BadRequestException("Title is required.");

            var userId = _userService.UserId;

            var board = new Domain.Board
            {
                Title = request.Title,
                OwnerUserId = userId
            };

            await _boardRepository.CreateAsync(board);

            // Persist first so board.Id exists for the BoardMember FK below.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Board access (IBoardAccessService.EnsureBoardOwnedByUserAsync, used for every
            // board-scoped read/write including loading the board's own columns/items) is
            // membership-based, not OwnerUserId-based — without this row the creator would be
            // locked out of the board they just created.
            var membership = new BoardMember
            {
                BoardId = board.Id,
                UserId = userId
            };

            await _boardMemberRepository.CreateAsync(membership);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BoardDto>(board);
        }
    }
}
