using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Mocks
{
    public class MockLeaveTypeRepository
    {
        public static Mock<ILeaveTypeRepository> GetMockLeaveTypeRepository()
        {
            var leaveTypes = new List<LeaveType>
            {
                new LeaveType
                {
                    Id= 1,
                    DefaultDays=10,
                    Name="Test Vacation"
                },
                new LeaveType
                {
                    Id= 2,
                    DefaultDays=15,
                    Name="Test Sick"
                },
                new LeaveType
                {
                    Id= 3,
                    DefaultDays=15,
                    Name="Test Maternity"
                }
            };

            var mockRepo = new Mock<ILeaveTypeRepository>();

            mockRepo.Setup(r => r.GetAsync()).ReturnsAsync(leaveTypes);

            mockRepo.Setup(r => r.CreateAsync(It.IsAny<LeaveType>()))
                .Returns((Delegate)((LeaveType leaveType) =>
                {
                    leaveTypes.Add(leaveType);
                    return Task.CompletedTask;
                }));

            return mockRepo;
        }
    }
}
