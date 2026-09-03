using AutoMapper;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.UnitTests.Mocks;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItem
{
    // Business-logic coverage for CreateBoardItemCommandHandler, complementing the
    // authorization-only assertions in BoardItemAuthorizationTests.
    public class CreateBoardItemCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidRequest_CreatesItemWithExpectedFieldsAndRecordsLifecycle()
        {
            Domain.BoardItem? captured = null;
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.AddBoardItem(It.IsAny<Domain.BoardItem>()))
                .Callback<Domain.BoardItem>(b => { b.Id = 55; captured = b; })
                .Returns(Task.CompletedTask);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("user-1");
            var lifecycleService = new Mock<ITaskLifecycleService>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<BoardItemDto>(It.IsAny<Domain.BoardItem>()))
                .Returns(new BoardItemDto { Id = 55, Title = "Practice scales" });

            var handler = new CreateBoardItemCommandHandler(
                repo.Object, accessService.Object, mapper.Object, userService.Object, lifecycleService.Object, unitOfWork.Object);

            var command = new CreateBoardItemCommand
            {
                Title = "Practice scales",
                Description = "C major",
                BoardColumnId = 3,
                BoardId = 10,
                StatusId = 1,
                PriorityId = 2
            };

            var result = await handler.Handle(command, CancellationToken.None);

            captured.ShouldNotBeNull();
            captured!.Title.ShouldBe("Practice scales");
            captured.Description.ShouldBe("C major");
            captured.BoardColumnId.ShouldBe(3);
            captured.BoardId.ShouldBe(10);
            captured.CreatedBy.ShouldBe("user-1");
            captured.BoardItemStatus.StatusId.ShouldBe(1);
            captured.BoardItemPriority.PriorityId.ShouldBe(2);

            lifecycleService.Verify(l => l.EnsureCreatedAsync(55), Times.Once);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            result.Id.ShouldBe(55);
        }

        [Fact]
        public async Task Handle_RecordsLifecycle_UsingIdAssignedByAddBoardItem_BeforeSavingChanges()
        {
            // EnsureCreatedAsync needs the item's real Id, which AddBoardItem only assigns once it
            // runs - if the handler read boardItem.Id beforehand it would record against the
            // default value (0) instead. Track call order to confirm Add -> lifecycle -> save.
            var callOrder = new List<string>();
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.AddBoardItem(It.IsAny<Domain.BoardItem>()))
                .Callback<Domain.BoardItem>(b => { b.Id = 7; callOrder.Add("Add"); })
                .Returns(Task.CompletedTask);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("user-1");
            var lifecycleService = new Mock<ITaskLifecycleService>();
            lifecycleService.Setup(l => l.EnsureCreatedAsync(7))
                .Callback(() => callOrder.Add("Lifecycle"))
                .Returns(Task.CompletedTask);
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => callOrder.Add("Save"))
                .ReturnsAsync(1);
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<BoardItemDto>(It.IsAny<Domain.BoardItem>())).Returns(new BoardItemDto { Id = 7 });

            var handler = new CreateBoardItemCommandHandler(
                repo.Object, accessService.Object, mapper.Object, userService.Object, lifecycleService.Object, unitOfWork.Object);

            await handler.Handle(new CreateBoardItemCommand { Title = "Item", BoardId = 1 }, CancellationToken.None);

            callOrder.ShouldBe(new[] { "Add", "Lifecycle", "Save" });
        }
    }
}
