using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoardItemsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUserService _userService;

        public BoardItemsController(IMediator mediator, IUserService userService)
        {
            _mediator = mediator;
            _userService = userService;
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

        [Authorize]
        [HttpPost("Create")]
        public async Task<ActionResult<BoardItemDto>> Create(CreateBoardItemCommand command)
        {
            var item = await _mediator.Send(command);
            return Ok(item);
        }

        [Authorize]
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


        [HttpPost("SwitchColumn")]
        public async Task<ActionResult<BoardItemDto>> SwitchColumn(UpdateBoardItemCommand command)
        {
            var updatedItem = await _mediator.Send(command);
            return Ok(updatedItem);
        }

        [HttpGet("GetUserFullName/{userId}")]
        public async Task<ActionResult<string>> GetUserFullName(string userId)
        {
            var user = await _userService.GetEmployee(userId);
            if (user == null)
                return NotFound($"User with ID {userId} not found.");

            return Ok($"{user.Firstname} {user.Lastname}");
        }

    }
}
