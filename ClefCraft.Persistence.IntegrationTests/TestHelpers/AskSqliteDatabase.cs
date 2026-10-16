using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ChangeAskState;
using ClefCraft.Application.Features.BoardItemAsks.Commands.CreateAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.NudgeAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ReassignAsk;
using ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Application.Models.Notifications;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
using ClefCraft.Persistence.Seeding;
using ClefCraft.Persistence.Services;
using ClefCraft.Persistence.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace ClefCraft.Persistence.IntegrationTests.TestHelpers
{
    /// <summary>
    /// A relational database (in-memory SQLite, one shared connection) with one board, one item and
    /// its members, for running the ask handlers against the real context, repositories, unit of
    /// work and activity logger. Each <see cref="Request"/> is one simulated HTTP request: its own
    /// DbContext and user, optionally with interceptors and SQL capture.
    /// </summary>
    public sealed class AskSqliteDatabase : IDisposable
    {
        public const string Owner = "user-owner";
        public const string Requester = "user-requester";
        public const string Recipient = "user-recipient";
        public const string Assignee = "user-assignee";
        public const string Other = "user-other";

        private readonly SqliteConnection _connection;

        private AskSqliteDatabase()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }

        public int BoardId { get; private set; }
        public int ItemId { get; private set; }
        public int ColumnId { get; private set; }
        public int InProgressStatusId { get; private set; }
        public int DoneStatusId { get; private set; }

        public static async Task<AskSqliteDatabase> CreateAsync()
        {
            var db = new AskSqliteDatabase();

            using var context = db.NewContext("seed-user");
            context.Database.EnsureCreated();

            var statuses = StandardReferenceData.StandardStatuses.Select(name => new Status { Name = name }).ToList();
            context.Statuses.AddRange(statuses);

            var board = new Board { Title = "Release", OwnerUserId = Owner };
            context.Boards.Add(board);
            await context.SaveChangesAsync();

            foreach (var member in new[] { Owner, Requester, Recipient, Assignee, Other })
                context.BoardMembers.Add(new BoardMember { BoardId = board.Id, UserId = member });

            var column = new BoardColumn { Title = "In Progress", BoardItems = new List<BoardItem>() };
            context.BoardColumnMappings.Add(new BoardColumnMapping { BoardId = board.Id, BoardColumn = column });
            await context.SaveChangesAsync();

            db.InProgressStatusId = statuses.Single(s => s.Name == "In Progress").Id;
            db.DoneStatusId = statuses.Single(s => s.Name == "Done").Id;

            var item = new BoardItem
            {
                BoardId = board.Id,
                BoardColumnId = column.Id,
                Title = "Release notes",
                AssigneeId = Assignee,
                BoardItemStatus = new BoardItemStatus { StatusId = db.InProgressStatusId }
            };
            context.BoardItems.Add(item);
            await context.SaveChangesAsync();

            db.BoardId = board.Id;
            db.ColumnId = column.Id;
            db.ItemId = item.Id;
            return db;
        }

        public ClefCraftDatabaseContext NewContext(string userId, List<string>? sql = null, params IInterceptor[] interceptors)
        {
            var builder = new DbContextOptionsBuilder<ClefCraftDatabaseContext>()
                .UseSqlite(_connection)
                .AddInterceptors(interceptors);

            if (sql != null)
                builder.LogTo(sql.Add, new[] { RelationalEventId.CommandExecuted });

            return new ClefCraftDatabaseContext(builder.Options, DatabaseContextFactory.CreateUserServiceMock(userId).Object);
        }

        public AskRequest Request(string userId, List<string>? sql = null, params IInterceptor[] interceptors) =>
            new(this, NewContext(userId, sql, interceptors), userId);

        /// <summary>Adds an ask directly (as if made earlier), at revision 1.</summary>
        public async Task<BoardItemAsk> AddAskAsync(
            BoardItemAskState state,
            string recipient = Recipient,
            string requester = Requester,
            bool isRequired = false)
        {
            using var context = NewContext(requester);
            var ask = new BoardItemAsk
            {
                BoardItemId = ItemId,
                RequestedByUserId = requester,
                RecipientUserId = recipient,
                Note = "Review the migration",
                State = state,
                IsRequired = isRequired,
                Revision = 1,
                RespondedAt = state is BoardItemAskState.Accepted or BoardItemAskState.Declined ? DateTime.UtcNow.AddDays(-1) : null,
                CompletedAt = state == BoardItemAskState.Completed ? DateTime.UtcNow.AddHours(-1) : null,
                CompletedByUserId = state == BoardItemAskState.Completed ? recipient : null
            };
            context.BoardItemAsks.Add(ask);
            await context.SaveChangesAsync();
            return ask;
        }

        /// <summary>A fresh, untracked read of the ask.</summary>
        public async Task<BoardItemAsk> AskAsync(int askId)
        {
            using var context = NewContext("reader");
            return await context.BoardItemAsks.AsNoTracking().SingleAsync(a => a.Id == askId);
        }

        public async Task<BoardItem> ItemAsync()
        {
            using var context = NewContext("reader");
            return await context.BoardItems.Include(i => i.BoardItemStatus).AsNoTracking().SingleAsync(i => i.Id == ItemId);
        }

        public async Task<List<ActivityLog>> ItemHistoryAsync(string? actionType = null)
        {
            using var context = NewContext("reader");
            return await context.ActivityLogs.AsNoTracking()
                .Where(l => l.EntityType == "BoardItem" && l.EntityId == ItemId && (actionType == null || l.ActionType == actionType))
                .ToListAsync();
        }

        public async Task<bool> IsMemberAsync(string userId)
        {
            using var context = NewContext("reader");
            return await context.BoardMembers.AnyAsync(m => m.BoardId == BoardId && m.UserId == userId);
        }

        public void Dispose() => _connection.Dispose();
    }

    /// <summary>One simulated request: a context, its user and the handlers built on them.</summary>
    public sealed class AskRequest : IDisposable
    {
        private readonly AskSqliteDatabase _db;

        public AskRequest(AskSqliteDatabase db, ClefCraftDatabaseContext context, string userId)
        {
            _db = db;
            Context = context;
            UserService = new Mock<IUserService>();
            UserService.Setup(u => u.UserId).Returns(userId);
            UserService.Setup(u => u.GetUser(It.IsAny<string>()))
                .ReturnsAsync((string id) => new User { Id = id, Firstname = id, Lastname = "Tester", Email = $"{id}@test" });
            UserService.Setup(u => u.GetUsersByIds(It.IsAny<List<string>>()))
                .ReturnsAsync((List<string> ids) => ids.Select(id => new User { Id = id, Firstname = id, Lastname = "Tester", Email = $"{id}@test" }).ToList());
            UserService.Setup(u => u.GetAssignee(It.IsAny<string>())).ReturnsAsync((Assignee?)null);
        }

        public ClefCraftDatabaseContext Context { get; }
        public Mock<IUserService> UserService { get; }
        public Mock<INotificationHubService> Hub { get; } = new();

        public void ShouldHaveNotifiedNobody() =>
            Hub.Verify(h => h.SendAskEventAsync(It.IsAny<string>(), It.IsAny<AskNotification>(), It.IsAny<CancellationToken>()), Times.Never);

        private BoardItemAskRepository Asks => new(Context);
        private BoardItemRepository Items => new(Context);
        private BoardRepository Boards => new(Context);
        private StatusRepository Statuses => new(Context);
        private BoardMemberRepository Members => new(Context);
        private MembershipAccessService Access => new(Context);
        private ActivityLogger Logger => new(Context, UserService.Object);
        private EfUnitOfWork UnitOfWork => new(Context);

        public CreateAskCommandHandler CreateAsk() => new(
            Asks, Items, Boards, Statuses, Members, Access, UserService.Object, Logger, Hub.Object, UnitOfWork);

        public ChangeAskStateCommandHandler ChangeState() => new(
            Asks, Items, Boards, Statuses, Members, Access, UserService.Object, Logger, Hub.Object, UnitOfWork);

        public NudgeAskCommandHandler Nudge() => new(
            Asks, Items, Boards, Statuses, Members, Access, UserService.Object, Logger, Hub.Object, UnitOfWork);

        public ReassignAskCommandHandler Reassign() => new(
            Asks, Items, Boards, Statuses, Members, Access, UserService.Object, Logger, Hub.Object, UnitOfWork);

        public UpdateBoardItemCommandHandler UpdateItem() => new(
            Items, Access, Statuses, new PriorityRepository(Context), new TagRepository(Context), UserService.Object,
            new TaskLifecycleService(Context), UnitOfWork, Members, Asks, Boards, Logger, Hub.Object);

        public RemoveBoardMemberCommandHandler RemoveMember() => new(
            Members, Boards, Access, UnitOfWork, Asks, Logger, Hub.Object, UserService.Object);

        // ---- Shorthands for the commands the tests send ------------------------------------------

        public Task Accept(BoardItemAsk ask) => ChangeState().Handle(
            new ChangeAskStateCommand { AskId = ask.Id, ExpectedRevision = ask.Revision, Action = BoardItemAskAction.Accept }, CancellationToken.None);

        public Task Decline(BoardItemAsk ask) => ChangeState().Handle(
            new ChangeAskStateCommand { AskId = ask.Id, ExpectedRevision = ask.Revision, Action = BoardItemAskAction.Decline, Reason = "Busy" }, CancellationToken.None);

        public Task Cancel(BoardItemAsk ask) => ChangeState().Handle(
            new ChangeAskStateCommand { AskId = ask.Id, ExpectedRevision = ask.Revision, Action = BoardItemAskAction.Cancel }, CancellationToken.None);

        public Task Reopen(BoardItemAsk ask) => ChangeState().Handle(
            new ChangeAskStateCommand { AskId = ask.Id, ExpectedRevision = ask.Revision, Action = BoardItemAskAction.Reopen }, CancellationToken.None);

        public Task NudgeAsk(BoardItemAsk ask) => Nudge().Handle(new NudgeAskCommand { AskId = ask.Id }, CancellationToken.None);

        public Task CreateAskFor(string recipient, int? replacesAskId = null, bool isRequired = false) => CreateAsk().Handle(
            new CreateAskCommand { BoardItemId = _db.ItemId, RecipientUserId = recipient, Note = "Check the numbers", IsRequired = isRequired, ReplacesAskId = replacesAskId },
            CancellationToken.None);

        public Task ReassignTo(BoardItemAsk ask, string newRecipient) => Reassign().Handle(
            new ReassignAskCommand { AskId = ask.Id, ExpectedRevision = ask.Revision, NewRecipientUserId = newRecipient }, CancellationToken.None);

        /// <summary>Moves the item to Done, acknowledging the given asks at the revisions passed.</summary>
        public Task CompleteItem(IEnumerable<BoardItemAsk>? acknowledged = null, IEnumerable<int>? complete = null, IEnumerable<int>? overrides = null, string? title = null) =>
            UpdateItem().Handle(new UpdateBoardItemCommand
            {
                Id = _db.ItemId,
                BoardColumnId = _db.ColumnId,
                StatusId = _db.DoneStatusId,
                Title = title,
                AskClosure = acknowledged == null ? null : new AskClosureRequest
                {
                    AcknowledgedAsks = acknowledged.Select(a => new AcknowledgedAsk { AskId = a.Id, Revision = a.Revision }).ToList(),
                    CompleteAskIds = (complete ?? Enumerable.Empty<int>()).ToList(),
                    OverrideAskIds = (overrides ?? Enumerable.Empty<int>()).ToList()
                }
            }, CancellationToken.None);

        public Task RemoveFromBoard(string userId) => RemoveMember().Handle(
            new RemoveBoardMemberCommand { BoardId = _db.BoardId, UserId = userId, RequestingUserId = UserService.Object.UserId },
            CancellationToken.None);

        public void Dispose() => Context.Dispose();
    }

    /// <summary>The same rules as Infrastructure's BoardAccessService (membership, not ownership), over this context.</summary>
    public sealed class MembershipAccessService : IBoardAccessService
    {
        private readonly ClefCraftDatabaseContext _context;

        public MembershipAccessService(ClefCraftDatabaseContext context) => _context = context;

        public async Task EnsureBoardOwnedByUserAsync(int boardId, string userId)
        {
            if (!await _context.Boards.AnyAsync(b => b.Id == boardId))
                throw new NotFoundException(nameof(Board), boardId);

            if (!await _context.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == userId))
                throw new ForbiddenAccessException();
        }

        public async Task EnsureBoardItemOwnedByUserAsync(int boardItemId, string userId)
        {
            var item = await _context.BoardItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == boardItemId)
                ?? throw new NotFoundException(nameof(BoardItem), boardItemId);

            await EnsureBoardOwnedByUserAsync(item.BoardId, userId);
        }

        public async Task EnsureUserIsBoardOwnerAsync(int boardId, string userId)
        {
            var board = await _context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == boardId)
                ?? throw new NotFoundException(nameof(Board), boardId);

            if (board.OwnerUserId != userId)
                throw new ForbiddenAccessException();
        }
    }

    /// <summary>Runs a competing request just before this context's first save, once (as RefreshTokenTests does).</summary>
    public sealed class CompetingWriteInterceptor : SaveChangesInterceptor
    {
        private readonly Func<Task> _competingWrite;
        private bool _fired;

        public CompetingWriteInterceptor(Func<Task> competingWrite) => _competingWrite = competingWrite;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!_fired)
            {
                _fired = true;
                await _competingWrite();
            }

            return result;
        }
    }
}
