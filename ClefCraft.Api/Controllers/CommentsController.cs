using ClefCraft.Application.Common.Models;
using ClefCraft.Application.Features.Comments;
using ClefCraft.Application.Features.Comments.Commands.CreateComment;
using ClefCraft.Application.Features.Comments.Commands.DeleteComment;
using ClefCraft.Application.Features.Comments.Commands.UpdateComment;
using ClefCraft.Application.Features.Comments.Queries.GetCommentsForEntity;
using ClefCraft.Application.Features.Comments.Queries.GetMentionableUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : Controller
    {
        private readonly IMediator _mediator;

        public CommentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/Comments/BoardItem/88?pageNumber=1&pageSize=20
        [HttpGet("{entityType}/{entityId}")]
        public async Task<ActionResult<PagedResult<CommentDto>>> GetComments(
            string entityType, int entityId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetCommentsForEntityQuery
            {
                EntityType = entityType,
                EntityId = entityId,
                PageNumber = pageNumber,
                PageSize = pageSize
            });

            return Ok(result);
        }

        // GET: api/Comments/BoardItem/88/mentionable-users
        [HttpGet("{entityType}/{entityId}/mentionable-users")]
        public async Task<ActionResult<List<MentionableUserDto>>> GetMentionableUsers(string entityType, int entityId)
        {
            var result = await _mediator.Send(new GetMentionableUsersQuery
            {
                EntityType = entityType,
                EntityId = entityId
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CreateCommentCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CommentDto>> UpdateComment(int id, [FromBody] UpdateCommentCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            await _mediator.Send(new DeleteCommentCommand { Id = id });
            return NoContent();
        }
    }
}
