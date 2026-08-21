using ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity;
using Shouldly;

namespace ClefCraft.Application.UnitTests.Features.ActivityLogs
{
    public class GetActivityLogForEntityValidatorTests
    {
        private readonly GetActivityLogForEntityValidator _validator = new();

        [Fact]
        public async Task Validate_KnownEntityType_Passes()
        {
            var query = new GetActivityLogForEntityQuery { EntityType = "BoardItem", EntityId = 1, PageNumber = 1, PageSize = 20 };

            var result = await _validator.ValidateAsync(query);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public async Task Validate_UnknownEntityType_Fails()
        {
            var query = new GetActivityLogForEntityQuery { EntityType = "SomethingElse", EntityId = 1, PageNumber = 1, PageSize = 20 };

            var result = await _validator.ValidateAsync(query);

            result.IsValid.ShouldBeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task Validate_PageSizeOutOfBounds_Fails(int pageSize)
        {
            var query = new GetActivityLogForEntityQuery { EntityType = "BoardItem", EntityId = 1, PageNumber = 1, PageSize = pageSize };

            var result = await _validator.ValidateAsync(query);

            result.IsValid.ShouldBeFalse();
        }

        [Fact]
        public async Task Validate_PageNumberLessThanOne_Fails()
        {
            var query = new GetActivityLogForEntityQuery { EntityType = "BoardItem", EntityId = 1, PageNumber = 0, PageSize = 20 };

            var result = await _validator.ValidateAsync(query);

            result.IsValid.ShouldBeFalse();
        }
    }
}
