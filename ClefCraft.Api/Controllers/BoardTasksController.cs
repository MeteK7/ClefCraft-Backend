using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Application.Features.BoardTask.Commands.CreateBoardTask;
using ClefCraft.Application.Features.BoardTask.Queries.GetBoardTasks;
using ClefCraft.Application.Features.BoardTask.Queries.GetBoardTasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoardTasksController : Controller
    {
        private readonly IMediator _mediator;

        public BoardTasksController(IMediator mediator)
        {
            _mediator = mediator;
        }
        //GET: api/<TasksController>
        [HttpGet]
        public async Task<ActionResult<List<BoardColumnDto>>> Get(bool isLoggedInUser = false)
        {
            var BoardColumns = await _mediator.Send(new GetBoardTasksQuery());
            return Ok(BoardColumns);
        }
        [HttpPost]
        public async Task<ActionResult<BoardTaskDto>> Create(CreateBoardTaskCommand command)
        {
            var task = await _mediator.Send(command);
            return Ok(task);
        }
    }
}
