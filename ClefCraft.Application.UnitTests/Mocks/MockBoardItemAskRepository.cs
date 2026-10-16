using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using Moq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ClefCraft.Application.UnitTests.Mocks
{
    public static class MockBoardItemAskRepository
    {
        /// <summary>An ask repository for handlers whose tests don't involve asks: there are none.</summary>
        public static Mock<IBoardItemAskRepository> GetEmpty()
        {
            var mock = new Mock<IBoardItemAskRepository>();

            mock.Setup(r => r.GetByItemIdAsync(It.IsAny<int>()))
                .ReturnsAsync(() => new List<BoardItemAsk>());
            mock.Setup(r => r.GetByItemIdsReadOnlyAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(() => new List<BoardItemAsk>());
            mock.Setup(r => r.GetOpenByRecipientOnBoardAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(() => new List<BoardItemAsk>());
            mock.Setup(r => r.GetWithItemReadOnlyAsync(It.IsAny<Expression<Func<BoardItemAsk, bool>>>()))
                .ReturnsAsync(() => new List<BoardItemAsk>());

            return mock;
        }
    }
}
