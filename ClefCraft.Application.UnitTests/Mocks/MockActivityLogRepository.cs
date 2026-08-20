using ClefCraft.Application.Contracts.ActivityLogs;
using ClefCraft.Domain;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Mocks
{
    public class MockActivityLogRepository
    {
        public static Mock<IActivityLogRepository> GetMockActivityLogRepository(List<ActivityLog> logs)
        {
            var mockRepo = new Mock<IActivityLogRepository>();

            mockRepo.Setup(r => r.GetByEntityAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((string entityType, int entityId, int skip, int take) =>
                    logs.Where(l => l.EntityType == entityType && l.EntityId == entityId)
                        .OrderByDescending(l => l.Timestamp)
                        .Skip(skip)
                        .Take(take)
                        .ToList());

            mockRepo.Setup(r => r.CountByEntityAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((string entityType, int entityId) =>
                    logs.Count(l => l.EntityType == entityType && l.EntityId == entityId));

            return mockRepo;
        }
    }
}
