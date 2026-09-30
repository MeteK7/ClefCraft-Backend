using AutoMapper;
using ClefCraft.Application.Features.Board.Commands.CreateBoard;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
using ClefCraft.Persistence.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.IntegrationTests
{
    public class BoardColumnAndTaxonomyTests
    {
        // ── Columns ────────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateBoard_GivesTheBoardExactlyTheFiveDefaultColumns_InLaneOrder()
        {
            var context = DatabaseContextFactory.CreateContext("owner-1");
            var itemRepository = new BoardItemRepository(context);
            var handler = new CreateBoardCommandHandler(
                new BoardRepository(context),
                new BoardMemberRepository(context),
                new Mock<IMapper>().Object,
                DatabaseContextFactory.CreateUserServiceMock("owner-1").Object,
                new EfUnitOfWork(context),
                new GenericRepository<BoardColumnMapping>(context));

            await handler.Handle(new CreateBoardCommand { Title = "Scales" }, CancellationToken.None);
            var boardId = context.Boards.Single().Id;

            var columns = await itemRepository.GetBoardColumnsWithBoardItems(boardId);

            columns.Select(c => c.Title).ShouldBe(new[] { "Backlog", "To Do", "In Progress", "In Review", "Done" });
            (await new BoardMemberRepository(context).IsMemberAsync(boardId, "owner-1")).ShouldBeTrue();
        }

        [Fact]
        public async Task IsColumnOnBoard_IsTrueOnlyForTheBoardsOwnColumns()
        {
            var context = DatabaseContextFactory.CreateContext();
            var boardA = new Board { Title = "A", OwnerUserId = "u" };
            var boardB = new Board { Title = "B", OwnerUserId = "u" };
            context.Boards.AddRange(boardA, boardB);
            await context.SaveChangesAsync();
            var columnOfA = new BoardColumnMapping { BoardId = boardA.Id, BoardColumn = new BoardColumn { Title = "To Do" } };
            var columnOfB = new BoardColumnMapping { BoardId = boardB.Id, BoardColumn = new BoardColumn { Title = "To Do" } };
            context.BoardColumnMappings.AddRange(columnOfA, columnOfB);
            await context.SaveChangesAsync();
            var repository = new BoardItemRepository(context);

            (await repository.IsColumnOnBoardAsync(boardA.Id, columnOfA.BoardColumnId)).ShouldBeTrue();
            (await repository.IsColumnOnBoardAsync(boardA.Id, columnOfB.BoardColumnId)).ShouldBeFalse();
            (await repository.IsColumnOnBoardAsync(boardA.Id, 999)).ShouldBeFalse();
        }

        // ── Status / Priority availability (BoardId null = every board, BoardId = X = board X) ──
        // On EF's SQLite provider rather than InMemory, so Distinct() runs as real SQL DISTINCT
        // the way it does on Postgres.

        private static ClefCraftDatabaseContext SqliteContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<ClefCraftDatabaseContext>().UseSqlite(connection).Options;
            var context = new ClefCraftDatabaseContext(options, DatabaseContextFactory.CreateUserServiceMock().Object);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task Statuses_GlobalOnesOnEveryBoard_BoardSpecificOnesOnlyOnTheirBoard_NoDuplicates()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = SqliteContext(connection);

            var board1 = new Board { Title = "One", OwnerUserId = "u" };
            var board2 = new Board { Title = "Two", OwnerUserId = "u" };
            var global = new Status { Name = "Global" };
            var onlyOne = new Status { Name = "Only board 1" };
            var onlyTwo = new Status { Name = "Only board 2" };
            var both = new Status { Name = "Global and board 1" };
            context.AddRange(board1, board2, global, onlyOne, onlyTwo, both);
            await context.SaveChangesAsync();
            context.BoardStatuses.AddRange(
                new BoardStatus { BoardId = null, StatusId = global.Id },
                new BoardStatus { BoardId = board1.Id, StatusId = onlyOne.Id },
                new BoardStatus { BoardId = board2.Id, StatusId = onlyTwo.Id },
                new BoardStatus { BoardId = null, StatusId = both.Id },
                new BoardStatus { BoardId = board1.Id, StatusId = both.Id });
            await context.SaveChangesAsync();
            var repository = new StatusRepository(context);

            (await repository.GetStatusesByBoardIdAsync(board1.Id)).Select(s => s.Name).OrderBy(n => n)
                .ShouldBe(new[] { "Global", "Global and board 1", "Only board 1" });
            (await repository.GetStatusesByBoardIdAsync(board2.Id)).Select(s => s.Name).OrderBy(n => n)
                .ShouldBe(new[] { "Global", "Global and board 1", "Only board 2" });
        }

        [Fact]
        public async Task Priorities_GlobalOnesOnEveryBoard_BoardSpecificOnesOnlyOnTheirBoard_NoDuplicates()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = SqliteContext(connection);

            var board1 = new Board { Title = "One", OwnerUserId = "u" };
            var board2 = new Board { Title = "Two", OwnerUserId = "u" };
            var global = new Priority { Name = "Global" };
            var onlyTwo = new Priority { Name = "Only board 2" };
            context.AddRange(board1, board2, global, onlyTwo);
            await context.SaveChangesAsync();
            context.BoardPriorities.AddRange(
                new BoardPriority { BoardId = null, PriorityId = global.Id },
                new BoardPriority { BoardId = board2.Id, PriorityId = global.Id },
                new BoardPriority { BoardId = board2.Id, PriorityId = onlyTwo.Id });
            await context.SaveChangesAsync();
            var repository = new PriorityRepository(context);

            (await repository.GetPrioritiesByBoardIdAsync(board1.Id)).Select(p => p.Name).ShouldBe(new[] { "Global" });
            (await repository.GetPrioritiesByBoardIdAsync(board2.Id)).Select(p => p.Name).OrderBy(n => n)
                .ShouldBe(new[] { "Global", "Only board 2" });
        }
    }
}
