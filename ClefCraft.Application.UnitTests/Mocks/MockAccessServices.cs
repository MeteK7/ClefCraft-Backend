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

            if (authorized)
            {
                boardSetup.Returns(Task.CompletedTask);
                itemSetup.Returns(Task.CompletedTask);
            }
            else
            {
                boardSetup.ThrowsAsync(new ForbiddenAccessException());
                itemSetup.ThrowsAsync(new ForbiddenAccessException());
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

            if (authorized)
            {
                eventSetup.Returns(Task.CompletedTask);
                seriesSetup.Returns(Task.CompletedTask);
                attachmentSetup.Returns(Task.CompletedTask);
            }
            else
            {
                eventSetup.ThrowsAsync(new ForbiddenAccessException());
                seriesSetup.ThrowsAsync(new ForbiddenAccessException());
                attachmentSetup.ThrowsAsync(new ForbiddenAccessException());
            }

            return mock;
        }
    }
}
