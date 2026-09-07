using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Exceptions;
using Moq;

namespace ClefCraft.Application.UnitTests.Mocks
{
    public static class MockAccessServices
    {
        public static Mock<IBoardAccessService> GetMockBoardAccessService(bool authorized = true)
        {
            var mock = new Mock<IBoardAccessService>();

            var boardSetup = mock.Setup(s =>
                s.EnsureBoardOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()));
            var itemSetup = mock.Setup(s =>
                s.EnsureBoardItemOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()));
            var ownerSetup = mock.Setup(s =>
                s.EnsureUserIsBoardOwnerAsync(It.IsAny<int>(), It.IsAny<string>()));

            if (authorized)
            {
                boardSetup.Returns(Task.CompletedTask);
                itemSetup.Returns(Task.CompletedTask);
                ownerSetup.Returns(Task.CompletedTask);
            }
            else
            {
                boardSetup.ThrowsAsync(new ForbiddenAccessException());
                itemSetup.ThrowsAsync(new ForbiddenAccessException());
                ownerSetup.ThrowsAsync(new ForbiddenAccessException());
            }

            return mock;
        }

        public static Mock<ICalendarAccessService> GetMockCalendarAccessService(bool authorized = true)
        {
            var mock = new Mock<ICalendarAccessService>();

            var eventSetup = mock.Setup(s =>
                s.EnsureEventOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()));
            var seriesSetup = mock.Setup(s =>
                s.EnsureSeriesOwnedByUserAsync(It.IsAny<string>(), It.IsAny<string>()));
            var attachmentSetup = mock.Setup(s =>
                s.EnsureAttachmentOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()));
            var canAccessEventSetup = mock.Setup(s =>
                s.EnsureCanAccessEventAsync(It.IsAny<int>(), It.IsAny<string>()));
            var canAccessAttachmentSetup = mock.Setup(s =>
                s.EnsureCanAccessAttachmentAsync(It.IsAny<int>(), It.IsAny<string>()));

            if (authorized)
            {
                eventSetup.Returns(Task.CompletedTask);
                seriesSetup.Returns(Task.CompletedTask);
                attachmentSetup.Returns(Task.CompletedTask);
                canAccessEventSetup.Returns(Task.CompletedTask);
                canAccessAttachmentSetup.Returns(Task.CompletedTask);
            }
            else
            {
                eventSetup.ThrowsAsync(new ForbiddenAccessException());
                seriesSetup.ThrowsAsync(new ForbiddenAccessException());
                attachmentSetup.ThrowsAsync(new ForbiddenAccessException());
                canAccessEventSetup.ThrowsAsync(new ForbiddenAccessException());
                canAccessAttachmentSetup.ThrowsAsync(new ForbiddenAccessException());
            }

            // GrantCollaboratorAccessAsync never throws by design (it's a silent no-op for a
            // non-owner granter) — default it to "nothing granted" so tests that don't care
            // about the grant side-effect aren't forced to set this up themselves.
            mock.Setup(s => s.GrantCollaboratorAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<string>());

            return mock;
        }
    }
}
