using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Features.Board.Queries.GetBoards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BoardsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUserService _userService;

        public BoardsController(IMediator mediator, IUserService userService)
        {
            _mediator = mediator;
            _userService = userService;
        }

        //GET: api/<BoardsController>
        [HttpGet]
        public async Task<ActionResult<List<BoardDto>>> Get()
        {
            var boards = await _mediator.Send(new GetBoardsQuery { UserId = _userService.UserId });
            return Ok(boards);
        }
    }
}
