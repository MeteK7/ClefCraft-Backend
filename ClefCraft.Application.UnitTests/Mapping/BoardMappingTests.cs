using ClefCraft.Domain;
using Shouldly;

namespace ClefCraft.Application.UnitTests.Mapping
{
    public class BoardMappingTests
    {
        private static readonly DateTime Created = new(2026, 5, 1, 9, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime Modified = new(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Board_MapsIdTitleAndOwner()
        {
            var dto = MappingUnderTest.ToBoardDto(new Board { Id = 3, Title = "Practice", OwnerUserId = "user-1" });

            dto.Id.ShouldBe(3);
            dto.Title.ShouldBe("Practice");
            dto.OwnerUserId.ShouldBe("user-1");
        }

        [Fact]
        public void Boards_MapsEachInOrder()
        {
            var dtos = MappingUnderTest.ToBoardDtos(new[]
            {
                new Board { Id = 1, Title = "A", OwnerUserId = "u" },
                new Board { Id = 2, Title = "B", OwnerUserId = "u" },
            });

            dtos.Select(d => d.Id).ShouldBe(new[] { 1, 2 });
            dtos.Select(d => d.Title).ShouldBe(new[] { "A", "B" });
        }

        [Fact]
        public void BoardColumn_MapsColumnAndItsItemsWithTheirStatusPriorityAndTags()
        {
            var column = new BoardColumn
            {
                Id = 7,
                Title = "In Progress",
                BoardItems = new List<BoardItem>
                {
                    new()
                    {
                        Id = 11,
                        Title = "Scales",
                        BoardId = 3,
                        BoardColumnId = 7,
                        BoardItemStatus = new BoardItemStatus { Status = new Status { Id = 2, Name = "Doing" } },
                        BoardItemPriority = new BoardItemPriority { Priority = new Priority { Id = 4, Name = "High" } },
                        BoardItemTags = new List<BoardItemTag> { new() { Tag = new Tag { Id = 9, Name = "Technique" } } },
                        DateCreated = Created,
                        DateModified = Modified,
                    },
                    new() { Id = 12, Title = "Arpeggios", BoardId = 3, BoardColumnId = 7 },
                },
            };

            var dto = MappingUnderTest.ToBoardColumnDto(column);

            dto.Id.ShouldBe(7);
            dto.Title.ShouldBe("In Progress");
            dto.BoardItems.Select(i => i.Id).ShouldBe(new[] { 11, 12 });

            var first = dto.BoardItems[0];
            first.Title.ShouldBe("Scales");
            first.Status.ShouldNotBeNull();
            first.Status.Id.ShouldBe(2);
            first.Status.Name.ShouldBe("Doing");
            first.Priority.ShouldNotBeNull();
            first.Priority.Id.ShouldBe(4);
            first.Priority.Name.ShouldBe("High");
            first.Tags.Select(t => (t.Id, t.Name)).ShouldBe(new[] { (9, "Technique") });
            first.DateCreated.ShouldBe(Created);
            first.DateModified.ShouldBe(Modified);

            var second = dto.BoardItems[1];
            second.Status.ShouldBeNull();
            second.Priority.ShouldBeNull();
            second.Tags.ShouldBeEmpty();
        }

        [Fact]
        public void BoardColumn_WithNullItems_MapsToAnEmptyItemList()
        {
            var dto = MappingUnderTest.ToBoardColumnDto(new BoardColumn { Id = 7, Title = "Done", BoardItems = null! });

            dto.BoardItems.ShouldNotBeNull();
            dto.BoardItems.ShouldBeEmpty();
        }

        [Fact]
        public void BoardColumns_MapsEachInOrder()
        {
            var dtos = MappingUnderTest.ToBoardColumnDtos(new[]
            {
                new BoardColumn { Id = 1, Title = "To Do", BoardItems = new List<BoardItem>() },
                new BoardColumn { Id = 2, Title = "Done", BoardItems = new List<BoardItem>() },
            });

            dtos.Select(c => (c.Id, c.Title)).ShouldBe(new[] { (1, "To Do"), (2, "Done") });
        }
    }
}
