using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Application.UnitTests.Features.ActivityLogs
{
    public class GetActivityLogForEntityHandlerTests
    {
        private static Mock<IUserService> MockUserService(params User[] users)
        {
            var mock = new Mock<IUserService>();
            mock.Setup(u => u.GetUsersByIds(It.IsAny<List<string>>()))
                .ReturnsAsync(users.ToList());
            return mock;
        }

        [Fact]
        public async Task Handle_ReturnsMappedEntries_WithResolvedActorNameAndParsedChanges()
        {
            var logs = new List<ActivityLog>
            {
                new ActivityLog
                {
                    Id = 1,
                    EntityType = "BoardItem",
                    EntityId = 88,
                    ActionType = "UPDATED",
                    UserId = "user-1",
                    Timestamp = DateTime.UtcNow,
                    MetadataJson = "{\"StatusId\":{\"Old\":2,\"New\":3}}"
                }
            };

            var repo = MockActivityLogRepository.GetMockActivityLogRepository(logs);
            var userService = MockUserService(new User { Id = "user-1", Firstname = "Jane", Lastname = "Doe" });

            var handler = new GetActivityLogForEntityHandler(repo.Object, userService.Object);

            var result = await handler.Handle(
                new GetActivityLogForEntityQuery { EntityType = "BoardItem", EntityId = 88 },
                CancellationToken.None);

            result.Items.Count.ShouldBe(1);
            result.TotalCount.ShouldBe(1);

            var entry = result.Items.Single();
            entry.ActorFullName.ShouldBe("Jane Doe");
            entry.Changes.Single().FieldName.ShouldBe("StatusId");
        }

        [Fact]
        public async Task Handle_EmptyRepositoryResult_ReturnsEmptyPage()
        {
            var repo = MockActivityLogRepository.GetMockActivityLogRepository(new List<ActivityLog>());
            var userService = MockUserService();

            var handler = new GetActivityLogForEntityHandler(repo.Object, userService.Object);

            var result = await handler.Handle(
                new GetActivityLogForEntityQuery { EntityType = "BoardItem", EntityId = 999 },
                CancellationToken.None);

            result.Items.ShouldBeEmpty();
            result.TotalCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_UnresolvedUser_FallsBackToUnknown()
        {
            var logs = new List<ActivityLog>
            {
                new ActivityLog
                {
                    Id = 1,
                    EntityType = "BoardItem",
                    EntityId = 88,
                    ActionType = "CREATED",
                    UserId = "missing-user",
                    Timestamp = DateTime.UtcNow,
                    MetadataJson = null
                }
            };

            var repo = MockActivityLogRepository.GetMockActivityLogRepository(logs);
            var userService = MockUserService(); // no matching user returned

            var handler = new GetActivityLogForEntityHandler(repo.Object, userService.Object);

            var result = await handler.Handle(
                new GetActivityLogForEntityQuery { EntityType = "BoardItem", EntityId = 88 },
                CancellationToken.None);

            result.Items.Single().ActorFullName.ShouldBe("Unknown");
            result.Items.Single().Changes.ShouldBeEmpty();
        }

        [Fact]
        public async Task Handle_UnknownEntityType_ThrowsBadRequestException()
        {
            var repo = MockActivityLogRepository.GetMockActivityLogRepository(new List<ActivityLog>());
            var userService = MockUserService();

            var handler = new GetActivityLogForEntityHandler(repo.Object, userService.Object);

            await Should.ThrowAsync<BadRequestException>(() =>
                handler.Handle(
                    new GetActivityLogForEntityQuery { EntityType = "NotAllowed", EntityId = 1 },
                    CancellationToken.None));
        }
    }
}
