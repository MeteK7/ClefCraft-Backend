using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClefCraft.Persistence.IntegrationTests
{
    public class ClefCraftDatabaseContextTests
    {
        private ClefCraftDatabaseContext _ClefCraftDatabaseContext;

        public ClefCraftDatabaseContextTests()
        {
            var dbOptions = new DbContextOptionsBuilder<ClefCraftDatabaseContext>()
                 .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

            //_ClefCraftDatabaseContext=new ClefCraftDatabaseContext(dbOptions); It gives error.
        }

        [Fact]
        public async void Save_SetDateCreatedValue()
        {
            //Arrange
            var leaveType = new LeaveType
            {
                Id = 1,
                DefaultDays = 10,
                Name = "Test Vacation"
            };

            //Act
            await _ClefCraftDatabaseContext.LeaveTypes.AddAsync(leaveType);
            await _ClefCraftDatabaseContext.SaveChangesAsync();

            //Assert
            leaveType.DateCreated.ShouldNotBeNull();
        }

        [Fact]
        public async void Save_SetDateModifiedValue()
        {
            //Arrange
            var leaveType = new LeaveType
            {
                Id = 1,
                DefaultDays = 10,
                Name = "Test Vacation"
            };

            //Act
            await _ClefCraftDatabaseContext.LeaveTypes.AddAsync(leaveType);
            await _ClefCraftDatabaseContext.SaveChangesAsync();

            //Assert
            leaveType.DateModified.ShouldNotBeNull();
        }
    }
}