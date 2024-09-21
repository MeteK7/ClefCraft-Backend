using ClefCraft.Application.Features.Board.Queries.GetBoards;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoardsController : Controller
    {
        private readonly IMediator _mediator;

        public BoardsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        //GET: api/<BoardsController> 
        [HttpGet]
        public async Task<ActionResult<List<BoardDto>>> Get()
        {
            var boards = await _mediator.Send(new GetBoardsQuery());
            return Ok(boards);
        }
    }
}
