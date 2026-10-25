using ClefCraft.Api.Models;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.AddAskReply;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ChangeAskState;
using ClefCraft.Application.Features.BoardItemAsks.Commands.CreateAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.EditAskReply;
using ClefCraft.Application.Features.BoardItemAsks.Commands.NudgeAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ReassignAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.SetAskExpectsAnswer;
using ClefCraft.Application.Features.BoardItemAsks.Commands.UpdateAsk;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetAsksForItem;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClefCraft.Api.Controllers
{
    // Asks on board items. Every action returns the ask as it now is (200), like the other board
    // endpoints; the caller is always the authenticated user (IUserService in the handlers).
    [Authorize]
    [ApiController]
    public class BoardItemAsksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BoardItemAsksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        //----------------------------------------
        // ON AN ITEM
        //----------------------------------------

        // Every ask on the item, in any state: what the panel and the completion prompt show.
        [HttpGet("api/BoardItems/{itemId:int}/asks")]
        public async Task<ActionResult<List<BoardItemAskDto>>> GetForItem(int itemId)
        {
            return Ok(await _mediator.Send(new GetAsksForItemQuery(itemId)));
        }

        [HttpPost("api/BoardItems/{itemId:int}/asks")]
        public async Task<ActionResult<BoardItemAskDto>> Create(int itemId, [FromBody] CreateAskRequest request)
        {
            return Ok(await _mediator.Send(new CreateAskCommand
            {
                BoardItemId = itemId,
                RecipientUserId = request.RecipientUserId,
                Note = request.Note,
                DueDate = request.DueDate,
                IsRequired = request.IsRequired,
                ExpectsAnswer = request.ExpectsAnswer,
                ReplacesAskId = request.ReplacesAskId
            }));
        }

        //----------------------------------------
        // THE CALLER'S ASKS
        //----------------------------------------

        // GET api/BoardItemAsks/mine?direction=received (or sent)
        [HttpGet("api/BoardItemAsks/mine")]
        public async Task<ActionResult<List<BoardItemAskDto>>> Mine([FromQuery, BindRequired] AskDirection direction)
        {
            return Ok(await _mediator.Send(new GetMyAsksQuery { Direction = direction }));
        }

        // GET api/BoardItemAsks/mine/count?direction=waiting: the "Waiting for you" badge
        [HttpGet("api/BoardItemAsks/mine/count")]
        public async Task<ActionResult<AskCountDto>> MineCount([FromQuery, BindRequired] AskDirection direction)
        {
            return Ok(new AskCountDto { Count = await _mediator.Send(new GetMyAskCountQuery { Direction = direction }) });
        }

        //----------------------------------------
        // ONE ASK
        //----------------------------------------

        [HttpPut("api/BoardItemAsks/{id:int}")]
        public async Task<ActionResult<BoardItemAskDto>> Update(int id, [FromBody] UpdateAskRequest request)
        {
            return Ok(await _mediator.Send(new UpdateAskCommand
            {
                AskId = id,
                ExpectedRevision = request.ExpectedRevision!.Value,
                Note = request.Note,
                DueDate = request.DueDate,
                IsRequired = request.IsRequired,
                WaiveReason = request.WaiveReason
            }));
        }

        [HttpPost("api/BoardItemAsks/{id:int}/accept")]
        public Task<ActionResult<BoardItemAskDto>> Accept(int id, [FromBody] AskStateChangeRequest request) =>
            ChangeState(id, BoardItemAskAction.Accept, request);

        [HttpPost("api/BoardItemAsks/{id:int}/decline")]
        public Task<ActionResult<BoardItemAskDto>> Decline(int id, [FromBody] AskStateChangeRequest request) =>
            ChangeState(id, BoardItemAskAction.Decline, request);

        [HttpPost("api/BoardItemAsks/{id:int}/complete")]
        public Task<ActionResult<BoardItemAskDto>> Complete(int id, [FromBody] AskStateChangeRequest request) =>
            ChangeState(id, BoardItemAskAction.Complete, request);

        [HttpPost("api/BoardItemAsks/{id:int}/reopen")]
        public Task<ActionResult<BoardItemAskDto>> Reopen(int id, [FromBody] AskStateChangeRequest request) =>
            ChangeState(id, BoardItemAskAction.Reopen, request);

        [HttpPost("api/BoardItemAsks/{id:int}/cancel")]
        public Task<ActionResult<BoardItemAskDto>> Cancel(int id, [FromBody] AskStateChangeRequest request) =>
            ChangeState(id, BoardItemAskAction.Cancel, request);

        [HttpPost("api/BoardItemAsks/{id:int}/nudge")]
        public async Task<ActionResult<BoardItemAskDto>> Nudge(int id)
        {
            return Ok(await _mediator.Send(new NudgeAskCommand { AskId = id }));
        }

        // Cancels this ask and asks someone else; returns the new ask.
        [HttpPost("api/BoardItemAsks/{id:int}/reassign")]
        public async Task<ActionResult<BoardItemAskDto>> Reassign(int id, [FromBody] ReassignAskRequest request)
        {
            return Ok(await _mediator.Send(new ReassignAskCommand
            {
                AskId = id,
                ExpectedRevision = request.ExpectedRevision!.Value,
                NewRecipientUserId = request.NewRecipientUserId,
                Note = request.Note,
                DueDate = request.DueDate
            }));
        }

        [HttpPost("api/BoardItemAsks/{id:int}/expects-answer")]
        public async Task<ActionResult<BoardItemAskDto>> SetExpectsAnswer(int id, [FromBody] SetAskExpectsAnswerRequest request)
        {
            return Ok(await _mediator.Send(new SetAskExpectsAnswerCommand
            {
                AskId = id,
                ExpectedRevision = request.ExpectedRevision!.Value,
                ExpectsAnswer = request.ExpectsAnswer!.Value
            }));
        }

        //----------------------------------------
        // REPLIES
        //----------------------------------------

        // Returns the ask with its replies, so the panel shows the new reply in place.
        [HttpPost("api/BoardItemAsks/{id:int}/replies")]
        public async Task<ActionResult<BoardItemAskDto>> AddReply(int id, [FromBody] AskReplyRequest request)
        {
            return Ok(await _mediator.Send(new AddAskReplyCommand { AskId = id, Body = request.Body }));
        }

        [HttpPut("api/BoardItemAskReplies/{replyId:int}")]
        public async Task<ActionResult<BoardItemAskDto>> EditReply(int replyId, [FromBody] AskReplyRequest request)
        {
            return Ok(await _mediator.Send(new EditAskReplyCommand { ReplyId = replyId, Body = request.Body }));
        }

        private async Task<ActionResult<BoardItemAskDto>> ChangeState(int id, BoardItemAskAction action, AskStateChangeRequest request)
        {
            return Ok(await _mediator.Send(new ChangeAskStateCommand
            {
                AskId = id,
                ExpectedRevision = request.ExpectedRevision!.Value,
                Action = action,
                Reason = request.Reason,
                ReplyBody = request.ReplyBody
            }));
        }
    }
}
