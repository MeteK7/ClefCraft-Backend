using AutoMapper;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemRelations.Commands.CreateRelation;
using ClefCraft.Application.Features.BoardItemRelations.Commands.DeleteRelation;
using ClefCraft.Application.Features.BoardItemRelations.Queries.GetRelations;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItemRelations
{
    // Regression coverage for the P0 authorization fix. Relation creation
    // previously never checked that either board item's board belonged to the
    // caller, letting cross-user relations be injected.
    public class BoardItemRelationsAuthorizationTests
    {
        private const string CallerUserId = "user-1";

        [Fact]
        public async Task GetRelations_ItemNotOwnedByCaller_ThrowsForbiddenAccessException()
        {
            var repo = new Mock<IBoardItemRelationRepository>();
            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new GetBoardItemRelationsQueryHandler(repo.Object, accessService.Object, userService.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetBoardItemRelationsQuery(1), CancellationToken.None));

            repo.Verify(r => r.GetForItemAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateRelation_TargetItemsBoardNotOwnedByCaller_ThrowsForbiddenAccessException_BeforeCreating()
        {
            var source = new Domain.BoardItem { Id = 1, BoardId = 10, Title = "Mine" };
            var target = new Domain.BoardItem { Id = 2, BoardId = 20, Title = "Someone else's" };

            var itemRepo = new Mock<IBoardItemRepository>();
            itemRepo.Setup(r => r.GetBoardItemById(1)).ReturnsAsync(source);
            itemRepo.Setup(r => r.GetBoardItemById(2)).ReturnsAsync(target);

            var relationRepo = new Mock<IBoardItemRelationRepository>();

            // Caller owns board 10 (source) but not board 20 (target).
            var accessService = new Mock<Application.Contracts.Authorization.IBoardAccessService>();
            accessService.Setup(s => s.EnsureBoardOwnedByUserAsync(10, CallerUserId)).Returns(Task.CompletedTask);
            accessService.Setup(s => s.EnsureBoardOwnedByUserAsync(20, CallerUserId)).ThrowsAsync(new ForbiddenAccessException());

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new CreateBoardItemRelationCommandHandler(
                relationRepo.Object,
                itemRepo.Object,
                accessService.Object,
                userService.Object,
                new Mock<IUnitOfWork>().Object,
                new Mock<IMapper>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(
                    new CreateBoardItemRelationCommand
                    {
                        SourceBoardItemId = 1,
                        TargetBoardItemId = 2,
                        RelationType = (int)BoardItemRelationType.Related
                    },
                    CancellationToken.None));

            relationRepo.Verify(r => r.AddAsync(It.IsAny<BoardItemRelation>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRelation_NotOwnedByCaller_ThrowsForbiddenAccessException_BeforeDeleting()
        {
            var relation = new BoardItemRelation { Id = 5, SourceBoardItemId = 1, TargetBoardItemId = 2, RelationType = BoardItemRelationType.Related };

            var relationRepo = new Mock<IBoardItemRelationRepository>();
            relationRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(relation);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new DeleteBoardItemRelationCommandHandler(
                relationRepo.Object,
                accessService.Object,
                userService.Object,
                new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new DeleteBoardItemRelationCommand { RelationId = 5 }, CancellationToken.None));

            relationRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
