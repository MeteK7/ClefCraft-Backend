using ClefCraft.Application.Features.BoardItemRelations.Commands.CreateRelation;
using ClefCraft.Application.Features.BoardItemRelations.Commands.DeleteRelation;
using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using ClefCraft.Application.Features.BoardItemRelations.Queries.GetRelations;
using ClefCraft.Application.Features.BoardItemRelations.Queries.SearchBoardItems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BoardItemRelationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BoardItemRelationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        //----------------------------------------
        // GET
        //----------------------------------------

        [HttpGet("{itemId}")]
        public async Task<ActionResult<RelationshipHubDto>> Get(int itemId)
        {
            var result =
                await _mediator.Send(
                    new GetBoardItemRelationsQuery(itemId));

            return Ok(result);
        }

        //----------------------------------------
        // SEARCH
        //----------------------------------------

        [HttpGet("search")]
        public async Task<ActionResult<List<BoardItemSearchDto>>> Search(
            [FromQuery] int boardId,
            [FromQuery] int excludeItemId,
            [FromQuery] string searchTerm)
        {
            var result =
                await _mediator.Send(
                    new SearchBoardItemsQuery(
                        boardId,
                        excludeItemId,
                        searchTerm));

            return Ok(result);
        }

        //----------------------------------------
        // CREATE
        //----------------------------------------

        [HttpPost]
        public async Task<ActionResult<int>> Create(
            CreateBoardItemRelationCommand command)
        {
            var id =
                await _mediator.Send(command);

            return Ok(id);
        }

        //----------------------------------------
        // DELETE
        //----------------------------------------

        [HttpDelete("{relationId}")]
        public async Task<IActionResult> Delete(int relationId)
        {
            await _mediator.Send(new DeleteBoardItemRelationCommand
            {
                RelationId = relationId
            });

            return NoContent();
        }
    }
}