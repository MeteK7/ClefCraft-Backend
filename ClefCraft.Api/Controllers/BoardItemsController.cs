using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoardItemsController : Controller
    {
        private readonly IMediator _mediator;

        public BoardItemsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        //GET: api/<ItemsController>
        [HttpGet]
        public async Task<ActionResult<List<BoardColumnDto>>> Get()
        {
            var BoardColumns = await _mediator.Send(new GetBoardItemsQuery());
            return Ok(BoardColumns);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BoardItemDto>> GetBoardItemById(int id)
        {
            var boardItem = await _mediator.Send(new GetBoardItemByIdQuery(id));
            if (boardItem == null)
            {
                return NotFound();
            }
            return Ok(boardItem);
        }

        [HttpPost("Create")]
        public async Task<ActionResult<BoardItemDto>> Create(CreateBoardItemCommand command)
        {
            var item = await _mediator.Send(command);
            return Ok(item);
        }

        [HttpPost("SwitchColumn")]
        public async Task<ActionResult<BoardItemDto>> SwitchColumn(UpdateBoardItemCommand command)
        {
            var updatedItem = await _mediator.Send(command);
            return Ok(updatedItem);
        }
    }
}
