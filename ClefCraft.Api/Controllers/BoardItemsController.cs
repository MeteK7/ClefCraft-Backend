using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem;
using ClefCraft.Application.Features.BoardItem.Commands.DeleteBoardItem;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.Features.BoardItem.Queries.GetUserFullName;
using ClefCraft.Application.Features.Priority.Queries.GetPriorities;
using ClefCraft.Application.Features.Status.Queries.GetStatuses;
using ClefCraft.Application.Features.Tag.Queries.GetTags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Authorize]
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
            var boardColumns = await _mediator.Send(new GetBoardItemsQuery());
            return Ok(boardColumns);
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

        [HttpGet("GetBoardItemsByBoardId/{boardId}")]
        public async Task<ActionResult<List<BoardColumnDto>>> GetBoardItemsByBoardId(int boardId)
        {
            var boardColumns = await _mediator.Send(new GetBoardItemsQuery(boardId));
            return Ok(boardColumns);
        }

        [HttpPost("Create")]
        public async Task<ActionResult<BoardItemDto>> Create(CreateBoardItemCommand command)
        {
            var item = await _mediator.Send(command);
            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BoardItemByIdDto>> Update(int id, UpdateBoardItemCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("Item ID mismatch.");
            }

            var updatedItem = await _mediator.Send(command);

            if (updatedItem == null)
            {
                return NotFound($"Board item with ID {id} not found.");
            }

            return Ok(updatedItem);
        }


        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteBoardItemCommand { Id = id });
            return NoContent();
        }

        [HttpPost("SwitchColumn")]
        public async Task<ActionResult<BoardItemDto>> SwitchColumn(UpdateBoardItemCommand command)
        {
            var updatedItem = await _mediator.Send(command);
            return Ok(updatedItem);
        }

        [HttpGet("GetUserFullName/{userId}")]
        public async Task<ActionResult<string>> GetUserFullName(string userId)
        {
            var fullName = await _mediator.Send(new GetUserFullNameQuery(userId));
            return Ok(fullName);
        }


        [HttpGet("GetTags")]
        public async Task<ActionResult<List<TagDto>>> GetTags([FromQuery] int boardId)
        {
            var boardColumns = await _mediator.Send(new GetTagsQuery()
            {
                BoardId=boardId
            });

            return Ok(boardColumns);
        }

        [HttpGet("GetStatuses")]
        public async Task<ActionResult<List<StatusDto>>> GetStatuses([FromQuery] int boardId)
        {
            return Ok(await _mediator.Send(new GetStatusesQuery
            {
                BoardId = boardId
            }));
        }

        [HttpGet("GetPriorities")]
        public async Task<ActionResult<List<PriorityDto>>> GetPriorities([FromQuery] int boardId)
        {
            return Ok(await _mediator.Send(new GetPrioritiesQuery
            {
                BoardId = boardId
            }));
        }
    }
}
