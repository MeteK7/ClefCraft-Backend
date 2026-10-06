using ClefCraft.Api.Models;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Features.BoardMembers.Commands.AddMember;
using ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember;
using ClefCraft.Application.Features.BoardMembers.DTOs;
using ClefCraft.Application.Features.BoardMembers.Queries.GetMembers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/Boards/{boardId}/Members")]
    public class BoardMembersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserService _userService;

        public BoardMembersController(IMediator mediator, IUserService userService)
        {
            _mediator = mediator;
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<List<BoardMemberDto>>> Get(int boardId)
        {
            var result = await _mediator.Send(new GetBoardMembersQuery
            {
                BoardId = boardId,
                UserId = _userService.UserId
            });

            return Ok(result);
        }

        // Binds a request model rather than the command itself: the command's RequestingUserId is
        // set here, and binding the command made model validation demand it from the body.
        [HttpPost]
        public async Task<ActionResult<BoardMemberDto>> Add(int boardId, [FromBody] AddBoardMemberRequest request)
        {
            var result = await _mediator.Send(new AddBoardMemberCommand
            {
                BoardId = boardId,
                UserId = request.UserId,
                RequestingUserId = _userService.UserId
            });

            return Ok(result);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> Remove(int boardId, string userId)
        {
            await _mediator.Send(new RemoveBoardMemberCommand
            {
                BoardId = boardId,
                UserId = userId,
                RequestingUserId = _userService.UserId
            });

            return NoContent();
        }
    }
}
