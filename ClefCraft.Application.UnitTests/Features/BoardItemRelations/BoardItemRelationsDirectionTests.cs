using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItemRelations.Commands.CreateRelation;
using ClefCraft.Application.Features.BoardItemRelations.Queries.GetRelations;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItemRelations
{
    // A relation is stored once (source -> target) but listed for both items. Each card has to say
    // which end the requested item is, or the client can't tell "blocks" from "blocked by".
    public class BoardItemRelationsDirectionTests
    {
        private const string CallerUserId = "user-1";

        private static readonly Domain.BoardItem Blocker = new() { Id = 1, BoardId = 10, Title = "Run migrations in the pipeline" };
        private static readonly Domain.BoardItem Launch = new() { Id = 2, BoardId = 10, Title = "Enable production deployment" };

        private static GetBoardItemRelationsQueryHandler HandlerFor(params BoardItemRelation[] relations)
        {
            var repo = new Mock<IBoardItemRelationRepository>();
            repo.Setup(r => r.GetForItemAsync(It.IsAny<int>())).ReturnsAsync(relations.ToList());

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            return new GetBoardItemRelationsQueryHandler(
                repo.Object,
                MockAccessServices.GetMockBoardAccessService(authorized: true).Object,
                userService.Object);
        }

        private static BoardItemRelation BlockerBlocksLaunch() => new()
        {
            Id = 7,
            SourceBoardItemId = Blocker.Id,
            SourceBoardItem = Blocker,
            TargetBoardItemId = Launch.Id,
            TargetBoardItem = Launch,
            RelationType = BoardItemRelationType.Blocks
        };

        [Fact]
        public async Task GetRelations_ForTheSource_ListsTheTargetAsOutgoing()
        {
            var hub = await HandlerFor(BlockerBlocksLaunch()).Handle(new GetBoardItemRelationsQuery(Blocker.Id), CancellationToken.None);

            var card = hub.Groups.Single(g => g.RelationType == (int)BoardItemRelationType.Blocks).Items.ShouldHaveSingleItem();
            card.ItemId.ShouldBe(Launch.Id);
            card.IsOutgoing.ShouldBeTrue();
            card.BoardId.ShouldBe(10);
            card.RelationId.ShouldBe(7);
        }

        [Fact]
        public async Task GetRelations_ForTheTarget_ListsTheSourceAsIncoming()
        {
            var hub = await HandlerFor(BlockerBlocksLaunch()).Handle(new GetBoardItemRelationsQuery(Launch.Id), CancellationToken.None);

            var card = hub.Groups.Single(g => g.RelationType == (int)BoardItemRelationType.Blocks).Items.ShouldHaveSingleItem();
            card.ItemId.ShouldBe(Blocker.Id);
            card.IsOutgoing.ShouldBeFalse();
            card.BoardId.ShouldBe(10);
        }

        [Fact]
        public async Task CreateRelation_ReturnsTheTargetAsOutgoing()
        {
            var itemRepo = new Mock<IBoardItemRepository>();
            itemRepo.Setup(r => r.GetBoardItemById(Blocker.Id)).ReturnsAsync(Blocker);
            itemRepo.Setup(r => r.GetBoardItemById(Launch.Id)).ReturnsAsync(Launch);

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new CreateBoardItemRelationCommandHandler(
                new Mock<IBoardItemRelationRepository>().Object,
                itemRepo.Object,
                MockAccessServices.GetMockBoardAccessService(authorized: true).Object,
                userService.Object,
                new Mock<IUnitOfWork>().Object);

            var card = await handler.Handle(
                new CreateBoardItemRelationCommand
                {
                    SourceBoardItemId = Blocker.Id,
                    TargetBoardItemId = Launch.Id,
                    RelationType = (int)BoardItemRelationType.Blocks
                },
                CancellationToken.None);

            card.ItemId.ShouldBe(Launch.Id);
            card.IsOutgoing.ShouldBeTrue();
            card.BoardId.ShouldBe(10);
        }
    }
}
