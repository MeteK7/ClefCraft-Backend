using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItemAsks.Commands.AddAskReply;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ChangeAskState;
using ClefCraft.Application.Features.BoardItemAsks.Commands.CreateAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.EditAskReply;
using ClefCraft.Application.Features.BoardItemAsks.Commands.NudgeAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ReassignAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.SetAskExpectsAnswer;
using ClefCraft.Application.Features.BoardItemAsks.Commands.UpdateAsk;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetAsksForItem;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks;
using ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Application.Models.Notifications;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public sealed record LoggedEntry(string EntityType, int EntityId, string ActionType, string ActorUserId, IReadOnlyDictionary<string, object> Metadata);

    public sealed record SentNotification(string UserId, AskNotification Payload);

    /// <summary>
    /// One board (10) with one item (1) and in-memory asks, behind mocks of every repository and
    /// service the ask handlers use. The repositories answer from the in-memory state, saves assign
    /// ids to new asks, and history entries and notifications are recorded for assertions.
    /// </summary>
    public sealed class AskTestBed
    {
        public const int BoardId = 10;
        public const int OtherBoardId = 20;
        public const int ItemId = 1;

        // The standard statuses in creation order: 1 Backlog, 2 To Do, 3 In Progress, 4 In Review, 5 Done.
        public const int InProgressStatusId = 3;
        public const int DoneStatusId = 5;

        public const string Owner = "user-owner";
        public const string Requester = "user-requester";
        public const string Recipient = "user-recipient";
        public const string Assignee = "user-assignee";
        public const string Other = "user-other";
        public const string Outsider = "user-outsider";

        private int _nextAskId = 100;
        private int _nextReplyId = 500;

        public AskTestBed()
        {
            Board = new Domain.Board { Id = BoardId, Title = "Release", OwnerUserId = Owner };
            Item = new Domain.BoardItem
            {
                Id = ItemId,
                BoardId = BoardId,
                Board = Board,
                Title = "Release notes",
                AssigneeId = Assignee,
                BoardColumnId = 1,
                BoardItemStatus = new BoardItemStatus { BoardItemId = ItemId, StatusId = InProgressStatusId }
            };

            Members = new HashSet<(int, string)>
            {
                (BoardId, Owner), (BoardId, Requester), (BoardId, Recipient), (BoardId, Assignee), (BoardId, Other)
            };

            SetUpRepositories();
            SetUpServices();
        }

        public Domain.Board Board { get; }
        public Domain.BoardItem Item { get; }
        public List<Domain.BoardItem> Items => new[] { Item }.Concat(ExtraItems).ToList();
        public List<Domain.BoardItem> ExtraItems { get; } = new();
        public List<BoardItemAsk> Asks { get; } = new();

        /// <summary>Saved replies, as the database holds them.</summary>
        public List<BoardItemAskReply> Replies { get; } = new();

        /// <summary>Replies added since the last save; a save inserts them, a failed save drops them.</summary>
        public List<BoardItemAskReply> PendingReplies { get; } = new();

        /// <summary>How often the replies were loaded, to show DTOs load them in one query.</summary>
        public int ReplyQueryCount { get; private set; }
        public HashSet<(int BoardId, string UserId)> Members { get; }

        /// <summary>The authenticated user (IUserService.UserId).</summary>
        public string CallerId { get; set; } = Requester;

        /// <summary>The next save throws ConcurrencyConflictException, as EfUnitOfWork does on a token mismatch.</summary>
        public bool FailNextSave { get; set; }

        public int SaveCount { get; private set; }
        public List<LoggedEntry> Logged { get; } = new();
        public List<SentNotification> Sent { get; } = new();

        public Mock<IBoardItemAskRepository> AskRepository { get; } = new();
        public Mock<IBoardItemRepository> BoardItemRepository { get; } = new();
        public Mock<IBoardRepository> BoardRepository { get; } = new();
        public Mock<IStatusRepository> StatusRepository { get; } = new();
        public Mock<IBoardMemberRepository> BoardMemberRepository { get; } = new();
        public Mock<IBoardAccessService> BoardAccessService { get; } = new();
        public Mock<IUserService> UserService { get; } = new();
        public Mock<IActivityLogger> ActivityLogger { get; } = new();
        public Mock<INotificationHubService> NotificationHub { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();

        public void MarkItemDone() => Item.BoardItemStatus.StatusId = DoneStatusId;

        public BoardItemAsk AddAsk(
            BoardItemAskState state,
            string recipient = Recipient,
            string requester = Requester,
            bool isRequired = false,
            Domain.BoardItem? item = null,
            DateTime? dateCreated = null)
        {
            item ??= Item;

            var ask = new BoardItemAsk
            {
                Id = _nextAskId++,
                BoardItemId = item.Id,
                BoardItem = item,
                RequestedByUserId = requester,
                RecipientUserId = recipient,
                Note = "Review the migration",
                IsRequired = isRequired,
                State = state,
                Revision = 3,
                DateCreated = dateCreated ?? DateTime.UtcNow.AddDays(-2),
                RespondedAt = state is BoardItemAskState.Accepted or BoardItemAskState.Declined ? DateTime.UtcNow.AddDays(-1) : null,
                CancelReason = state == BoardItemAskState.Cancelled ? BoardItemAskCancelReason.Withdrawn : null
            };

            if (state == BoardItemAskState.Completed)
            {
                ask.CompletedAt = DateTime.UtcNow.AddHours(-3);
                ask.CompletedByUserId = recipient;
            }

            Asks.Add(ask);
            return ask;
        }

        public Domain.BoardItem AddItemOnOtherBoard(string title = "Elsewhere")
        {
            var item = new Domain.BoardItem
            {
                Id = 2,
                BoardId = OtherBoardId,
                Board = new Domain.Board { Id = OtherBoardId, Title = "Other", OwnerUserId = Outsider },
                Title = title,
                BoardItemStatus = new BoardItemStatus { BoardItemId = 2, StatusId = InProgressStatusId }
            };
            ExtraItems.Add(item);
            return item;
        }

        /// <summary>A saved reply on the ask, posted at the given time.</summary>
        public BoardItemAskReply AddReply(BoardItemAsk ask, string author, string body = "SELECT 1;", DateTime? at = null)
        {
            var reply = new BoardItemAskReply
            {
                Id = _nextReplyId++,
                BoardItemAskId = ask.Id,
                AuthorUserId = author,
                Body = body,
                DateCreated = at ?? DateTime.UtcNow.AddMinutes(-5)
            };
            Replies.Add(reply);
            return reply;
        }

        public IEnumerable<LoggedEntry> LoggedOf(string actionType) => Logged.Where(l => l.ActionType == actionType);

        public IEnumerable<SentNotification> SentTo(string userId) => Sent.Where(s => s.UserId == userId);

        // ---- Handlers -------------------------------------------------------------------------

        public CreateAskCommandHandler CreateHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object, ActivityLogger.Object,
            NotificationHub.Object, UnitOfWork.Object);

        public UpdateAskCommandHandler UpdateHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object, ActivityLogger.Object,
            NotificationHub.Object, UnitOfWork.Object);

        public ChangeAskStateCommandHandler ChangeStateHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object, ActivityLogger.Object,
            NotificationHub.Object, UnitOfWork.Object);

        public NudgeAskCommandHandler NudgeHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object, ActivityLogger.Object,
            NotificationHub.Object, UnitOfWork.Object);

        public ReassignAskCommandHandler ReassignHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object, ActivityLogger.Object,
            NotificationHub.Object, UnitOfWork.Object);

        public AddAskReplyCommandHandler AddReplyHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object, NotificationHub.Object, UnitOfWork.Object);

        public EditAskReplyCommandHandler EditReplyHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object, UnitOfWork.Object);

        public SetAskExpectsAnswerCommandHandler SetExpectsAnswerHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object, ActivityLogger.Object, UnitOfWork.Object);

        public GetAsksForItemQueryHandler GetAsksForItemHandler() => new(
            AskRepository.Object, BoardItemRepository.Object, BoardRepository.Object, StatusRepository.Object,
            BoardMemberRepository.Object, BoardAccessService.Object, UserService.Object);

        public GetMyAsksQueryHandler GetMyAsksHandler() => new(
            AskRepository.Object, BoardMemberRepository.Object, StatusRepository.Object, UserService.Object);

        public GetMyAskCountQueryHandler GetMyAskCountHandler() => new(
            AskRepository.Object, BoardMemberRepository.Object, StatusRepository.Object, UserService.Object);

        public UpdateBoardItemCommandHandler UpdateItemHandler(Mock<ITaskLifecycleService>? lifecycle = null) => new(
            BoardItemRepository.Object, BoardAccessService.Object, StatusRepository.Object,
            new Mock<IPriorityRepository>().Object, new Mock<ITagRepository>().Object, UserService.Object,
            (lifecycle ?? new Mock<ITaskLifecycleService>()).Object, UnitOfWork.Object, BoardMemberRepository.Object,
            AskRepository.Object, BoardRepository.Object, ActivityLogger.Object, NotificationHub.Object);

        public RemoveBoardMemberCommandHandler RemoveMemberHandler() => new(
            BoardMemberRepository.Object, BoardRepository.Object, BoardAccessService.Object, UnitOfWork.Object,
            AskRepository.Object, ActivityLogger.Object, NotificationHub.Object, UserService.Object);

        // ---- Setup ----------------------------------------------------------------------------

        private Domain.BoardItem? ItemById(int id) => Items.FirstOrDefault(i => i.Id == id);

        private void SetUpRepositories()
        {
            AskRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => Asks.FirstOrDefault(a => a.Id == id));
            AskRepository.Setup(r => r.GetByItemIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int itemId) => Asks.Where(a => a.BoardItemId == itemId).ToList());
            AskRepository.Setup(r => r.GetByItemIdsReadOnlyAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> ids) => Asks.Where(a => ids.Contains(a.BoardItemId)).ToList());
            AskRepository.Setup(r => r.GetOpenByRecipientOnBoardAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int boardId, string userId) => Asks
                    .Where(a => a.BoardItem.BoardId == boardId && a.RecipientUserId == userId
                        && (a.State == BoardItemAskState.Pending || a.State == BoardItemAskState.Accepted))
                    .ToList());
            AskRepository.Setup(r => r.GetWithItemReadOnlyAsync(It.IsAny<Expression<Func<BoardItemAsk, bool>>>()))
                .ReturnsAsync((Expression<Func<BoardItemAsk, bool>> filter) => Asks.Where(filter.Compile()).ToList());
            AskRepository.Setup(r => r.CountAsync(It.IsAny<Expression<Func<BoardItemAsk, bool>>>()))
                .ReturnsAsync((Expression<Func<BoardItemAsk, bool>> filter) => Asks.Count(filter.Compile()));
            AskRepository.Setup(r => r.CreateAsync(It.IsAny<BoardItemAsk>()))
                .Callback((BoardItemAsk ask) =>
                {
                    ask.BoardItem ??= ItemById(ask.BoardItemId)!;
                    Asks.Add(ask);
                })
                .Returns(Task.CompletedTask);
            AskRepository.Setup(r => r.GetRepliesByAskIdsReadOnlyAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> ids) =>
                {
                    ReplyQueryCount++;
                    return Replies.Where(r => ids.Contains(r.BoardItemAskId)).OrderBy(r => r.Id).ToList();
                });
            AskRepository.Setup(r => r.GetReplyByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => Replies.FirstOrDefault(r => r.Id == id));
            AskRepository.Setup(r => r.AddReplyAsync(It.IsAny<BoardItemAskReply>()))
                .Callback((BoardItemAskReply reply) => PendingReplies.Add(reply))
                .Returns(Task.CompletedTask);

            BoardItemRepository.Setup(r => r.GetBoardItemById(It.IsAny<int>()))
                .ReturnsAsync((int id) => ItemById(id));
            BoardItemRepository.Setup(r => r.UpdateBoardItem(It.IsAny<Domain.BoardItem>()))
                .Returns(Task.CompletedTask);

            BoardRepository.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => Items.Select(i => i.Board).FirstOrDefault(b => b?.Id == id));

            StatusRepository.Setup(r => r.GetStatusesAsync()).ReturnsAsync(() =>
                new[] { "Backlog", "To Do", "In Progress", "In Review", "Done" }
                    .Select((name, i) => new Status { Id = i + 1, Name = name })
                    .ToList());

            BoardMemberRepository.Setup(r => r.IsMemberAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int boardId, string userId) => Members.Contains((boardId, userId)));
            BoardMemberRepository.Setup(r => r.GetByBoardIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int boardId) => MembersOf(new[] { boardId }));
            BoardMemberRepository.Setup(r => r.GetByBoardIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> boardIds) => MembersOf(boardIds));
            BoardMemberRepository.Setup(r => r.GetMemberBoardIdsAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) => Members.Where(m => m.UserId == userId).Select(m => m.BoardId).Distinct().ToList());
            BoardMemberRepository.Setup(r => r.GetByBoardAndUserAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int boardId, string userId) => Members.Contains((boardId, userId))
                    ? new BoardMember { BoardId = boardId, UserId = userId }
                    : null);
            BoardMemberRepository.Setup(r => r.DeleteAsync(It.IsAny<BoardMember>()))
                .Callback((BoardMember m) => Members.Remove((m.BoardId, m.UserId)))
                .Returns(Task.CompletedTask);

            UnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (FailNextSave)
                    {
                        FailNextSave = false;
                        PendingReplies.Clear();
                        throw new ConcurrencyConflictException(new InvalidOperationException("token mismatch"));
                    }

                    foreach (var reply in PendingReplies)
                    {
                        reply.Id = _nextReplyId++;
                        reply.DateCreated = DateTime.UtcNow;
                        Replies.Add(reply);
                    }
                    PendingReplies.Clear();

                    // As EF does: an answer linked by navigation gets its key once the reply has an id.
                    foreach (var ask in Asks.Where(a => a.AnswerReply != null))
                        ask.AnswerReplyId = ask.AnswerReply!.Id;

                    // What the database does on insert: an id, and DateCreated from SaveChangesAsync.
                    foreach (var ask in Asks.Where(a => a.Id == 0))
                    {
                        ask.Id = _nextAskId++;
                        ask.DateCreated = DateTime.UtcNow;
                    }

                    SaveCount++;
                    return 1;
                });
        }

        private List<BoardMember> MembersOf(IEnumerable<int> boardIds) =>
            Members.Where(m => boardIds.Contains(m.BoardId))
                .Select(m => new BoardMember { BoardId = m.BoardId, UserId = m.UserId })
                .ToList();

        private void SetUpServices()
        {
            BoardAccessService.Setup(s => s.EnsureBoardItemOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns((int itemId, string userId) =>
                {
                    var item = ItemById(itemId) ?? throw new NotFoundException(nameof(Domain.BoardItem), itemId);
                    if (!Members.Contains((item.BoardId, userId)))
                        throw new ForbiddenAccessException();
                    return Task.CompletedTask;
                });
            BoardAccessService.Setup(s => s.EnsureBoardOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns((int boardId, string userId) =>
                    Members.Contains((boardId, userId)) ? Task.CompletedTask : throw new ForbiddenAccessException());
            BoardAccessService.Setup(s => s.EnsureUserIsBoardOwnerAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns((int boardId, string userId) =>
                    boardId == BoardId && userId == Owner ? Task.CompletedTask : throw new ForbiddenAccessException());

            UserService.Setup(u => u.UserId).Returns(() => CallerId);
            UserService.Setup(u => u.GetUser(It.IsAny<string>()))
                .ReturnsAsync((string id) => MakeUser(id));
            UserService.Setup(u => u.GetUsersByIds(It.IsAny<List<string>>()))
                .ReturnsAsync((List<string> ids) => ids.Select(MakeUser).ToList());

            ActivityLogger.Setup(l => l.LogAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<object?>()))
                .Callback((string entityType, int entityId, string actionType, object? metadata) =>
                    Logged.Add(new LoggedEntry(entityType, entityId, actionType, CallerId,
                        (IReadOnlyDictionary<string, object>?)(metadata as Dictionary<string, object>) ?? new Dictionary<string, object>())))
                .Returns(Task.CompletedTask);

            NotificationHub.Setup(h => h.SendAskEventAsync(It.IsAny<string>(), It.IsAny<AskNotification>(), It.IsAny<CancellationToken>()))
                .Callback((string userId, AskNotification payload, CancellationToken _) => Sent.Add(new SentNotification(userId, payload)))
                .Returns(Task.CompletedTask);
        }

        // Each test user's name is their id, so assertions can name people by id.
        private static User MakeUser(string id) => new() { Id = id, Firstname = id, Lastname = "Tester", Email = $"{id}@test" };

        public static string FullName(string userId) => $"{userId} Tester";
    }
}
