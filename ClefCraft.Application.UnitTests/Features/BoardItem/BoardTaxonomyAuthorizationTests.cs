using AutoMapper;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Priority.Queries.GetPriorities;
using ClefCraft.Application.Features.Status.Queries.GetStatuses;
using ClefCraft.Application.Features.Tag.Queries.GetTags;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItem
{
    // Regression coverage for the P0 follow-up: GetTags/GetStatuses/GetPriorities let any
    // authenticated user enumerate another board's taxonomy by boardId, with no membership
    // check at all. Each handler must now reject a caller who isn't a member of the board.
    public class BoardTaxonomyAuthorizationTests
    {
        private const string CallerUserId = "user-1";
        private const int BoardId = 10;

        [Fact]
        public async Task GetTags_CallerNotBoardMember_ThrowsForbiddenAccessException()
        {
            var repo = new Mock<ITagRepository>();
            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new GetTagsHandler(repo.Object, accessService.Object, userService.Object, new Mock<IMapper>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetTagsQuery { BoardId = BoardId }, CancellationToken.None));

            repo.Verify(r => r.GetTagsByBoardIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetTags_CallerIsBoardMember_ReturnsMappedTags()
        {
            var tags = new List<Tag> { new Tag { Id = 1, Name = "urgent" } };
            var repo = new Mock<ITagRepository>();
            repo.Setup(r => r.GetTagsByBoardIdAsync(BoardId)).ReturnsAsync(tags);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<List<TagDto>>(tags)).Returns(new List<TagDto> { new TagDto { Id = 1, Name = "urgent" } });

            var handler = new GetTagsHandler(repo.Object, accessService.Object, userService.Object, mapper.Object);

            var result = await handler.Handle(new GetTagsQuery { BoardId = BoardId }, CancellationToken.None);

            result.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task GetStatuses_CallerNotBoardMember_ThrowsForbiddenAccessException()
        {
            var repo = new Mock<IStatusRepository>();
            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new GetStatusesHandler(repo.Object, accessService.Object, userService.Object, new Mock<IMapper>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetStatusesQuery { BoardId = BoardId }, CancellationToken.None));

            repo.Verify(r => r.GetStatusesByBoardIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetPriorities_CallerNotBoardMember_ThrowsForbiddenAccessException()
        {
            var repo = new Mock<IPriorityRepository>();
            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new GetPrioritiesHandler(repo.Object, accessService.Object, userService.Object, new Mock<IMapper>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetPrioritiesQuery { BoardId = BoardId }, CancellationToken.None));

            repo.Verify(r => r.GetPrioritiesByBoardIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
