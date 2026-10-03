using ClefCraft.Domain;
using Shouldly;

namespace ClefCraft.Application.UnitTests.Mapping
{
    public class BoardItemMappingTests
    {
        private static readonly DateTime Created = new(2026, 5, 1, 9, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime Modified = new(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Due = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        private static BoardItem FullItem() => new()
        {
            Id = 11,
            Title = "Scales",
            Description = "C major, two octaves",
            BoardId = 3,
            BoardColumnId = 7,
            AssigneeId = "user-2",
            DueDate = Due,
            EstimatedTime = 1.5,
            TimeSpent = 0.75,
            CreatedBy = "user-1",
            ModifiedBy = "user-3",
            DateCreated = Created,
            DateModified = Modified,
            BoardItemStatus = new BoardItemStatus { Status = new Status { Id = 2, Name = "Doing" } },
            BoardItemPriority = new BoardItemPriority { Priority = new Priority { Id = 4, Name = "High" } },
            BoardItemTags = new List<BoardItemTag>
            {
                new() { Tag = new Tag { Id = 9, Name = "Technique" } },
                new() { Tag = new Tag { Id = 10, Name = "Warm-up" } },
            },
        };

        /// <summary>No status, priority, tags, dates or optional values.</summary>
        private static BoardItem BareItem() => new() { Id = 12, Title = "Arpeggios", BoardId = 3, BoardColumnId = 7 };

        // ── BoardItem → BoardItemDto ────────────────────────────────────────────────

        [Fact]
        public void BoardItemDto_MapsAllSourceFields()
        {
            var dto = MappingUnderTest.ToBoardItemDto(FullItem());

            dto.Id.ShouldBe(11);
            dto.Title.ShouldBe("Scales");
            dto.Description.ShouldBe("C major, two octaves");
            dto.BoardId.ShouldBe(3);
            dto.BoardColumnId.ShouldBe(7);
            dto.AssigneeId.ShouldBe("user-2");
            dto.DueDate.ShouldBe(Due);
            dto.EstimatedTime.ShouldBe(1.5);
            dto.TimeSpent.ShouldBe(0.75);
            dto.CreatedBy.ShouldBe("user-1");
            dto.ModifiedBy.ShouldBe("user-3");
            dto.DateCreated.ShouldBe(Created);
            dto.DateModified.ShouldBe(Modified);
            dto.Status.Id.ShouldBe(2);
            dto.Status.Name.ShouldBe("Doing");
            dto.Priority.Id.ShouldBe(4);
            dto.Priority.Name.ShouldBe("High");
            dto.Tags.Select(t => (t.Id, t.Name)).ShouldBe(new[] { (9, "Technique"), (10, "Warm-up") });
        }

        [Fact]
        public void BoardItemDto_LeavesNameFieldsForTheHandlerToFill()
        {
            var dto = MappingUnderTest.ToBoardItemDto(FullItem());

            dto.AssigneeFirstName.ShouldBeNull();
            dto.AssigneeLastName.ShouldBeNull();
            dto.CreatedByFullName.ShouldBeNull();
            dto.ModifiedByFullName.ShouldBeNull();
        }

        [Fact]
        public void BoardItemDto_WithoutOptionalData_UsesNullsEmptyTagsAndDefaultDates()
        {
            var dto = MappingUnderTest.ToBoardItemDto(BareItem());

            dto.Description.ShouldBeNull();
            dto.AssigneeId.ShouldBeNull();
            dto.DueDate.ShouldBeNull();
            dto.EstimatedTime.ShouldBeNull();
            dto.TimeSpent.ShouldBeNull();
            dto.Status.ShouldBeNull();
            dto.Priority.ShouldBeNull();
            dto.Tags.ShouldBeEmpty();
            dto.DateCreated.ShouldBe(default(DateTime));
            dto.DateModified.ShouldBe(default(DateTime));
        }

        [Fact]
        public void BoardItemDto_WithStatusAndPriorityLinksButNoLoadedEntities_HasNullStatusAndPriority()
        {
            var item = BareItem();
            item.BoardItemStatus = new BoardItemStatus { StatusId = 2 };
            item.BoardItemPriority = new BoardItemPriority { PriorityId = 4 };

            var dto = MappingUnderTest.ToBoardItemDto(item);

            dto.Status.ShouldBeNull();
            dto.Priority.ShouldBeNull();
        }

        [Fact]
        public void BoardItemDto_WithNullTagCollection_HasEmptyTags()
        {
            var item = BareItem();
            item.BoardItemTags = null!;

            var dto = MappingUnderTest.ToBoardItemDto(item);

            dto.Tags.ShouldNotBeNull();
            dto.Tags.ShouldBeEmpty();
        }

        // ── BoardItem → BoardItemByIdDto ────────────────────────────────────────────

        [Fact]
        public void BoardItemByIdDto_MapsTheMatchingFields()
        {
            var dto = MappingUnderTest.ToBoardItemByIdDto(FullItem());

            dto.Id.ShouldBe(11);
            dto.Title.ShouldBe("Scales");
            dto.Description.ShouldBe("C major, two octaves");
            dto.AssigneeId.ShouldBe("user-2");
            dto.DueDate.ShouldBe(Due);
            dto.EstimatedTime.ShouldBe(1.5);
            dto.TimeSpent.ShouldBe(0.75);
        }

        [Fact]
        public void BoardItemByIdDto_HasNoSourceForStatusPriorityTagsOrNames()
        {
            // The entity has BoardItemStatus/BoardItemPriority/BoardItemTags, not Status/Priority/
            // Tags, and no assignee names; the mapping leaves these for the handler to fill.
            var dto = MappingUnderTest.ToBoardItemByIdDto(FullItem());

            dto.Status.ShouldBeNull();
            dto.Priority.ShouldBeNull();
            dto.Tags.ShouldBeEmpty();
            dto.AssigneeFirstName.ShouldBeNull();
            dto.AssigneeLastName.ShouldBeNull();
        }

        [Fact]
        public void BoardItemByIdDto_WithoutOptionalValues_UsesDefaults()
        {
            var dto = MappingUnderTest.ToBoardItemByIdDto(BareItem());

            dto.Description.ShouldBeNull();
            dto.AssigneeId.ShouldBeNull();
            dto.DueDate.ShouldBe(default(DateTime));
            dto.EstimatedTime.ShouldBe(0);
            dto.TimeSpent.ShouldBe(0);
        }

        // ── BoardItem → RelationshipCardDto ─────────────────────────────────────────

        [Fact]
        public void RelationshipCard_MapsItemAndNames()
        {
            var dto = MappingUnderTest.ToRelationshipCardDto(FullItem());

            dto.ItemId.ShouldBe(11);
            dto.RelationId.ShouldBe(0); // set by the handler
            dto.Title.ShouldBe("Scales");
            dto.Status.ShouldBe("Doing");
            dto.Priority.ShouldBe("High");
            dto.AssigneeId.ShouldBe("user-2");
            dto.DueDate.ShouldBe(Due);
        }

        [Fact]
        public void RelationshipCard_WithoutStatusOrPriority_UsesEmptyStrings()
        {
            var dto = MappingUnderTest.ToRelationshipCardDto(BareItem());

            dto.Status.ShouldBe("");
            dto.Priority.ShouldBe("");
            dto.AssigneeId.ShouldBeNull();
            dto.DueDate.ShouldBeNull();
        }

        [Fact]
        public void RelationshipCard_WithStatusAndPriorityLinksButNoLoadedEntities_HasNullNames()
        {
            var item = BareItem();
            item.BoardItemStatus = new BoardItemStatus { StatusId = 2 };
            item.BoardItemPriority = new BoardItemPriority { PriorityId = 4 };

            var dto = MappingUnderTest.ToRelationshipCardDto(item);

            dto.Status.ShouldBeNull();
            dto.Priority.ShouldBeNull();
        }

        // ── BoardItem → BoardItemSearchDto ──────────────────────────────────────────

        [Fact]
        public void SearchResults_MapIdTitleAndNamesInOrder()
        {
            var dtos = MappingUnderTest.ToBoardItemSearchDtos(new[] { FullItem(), BareItem() });

            dtos.Select(d => (d.Id, d.Title)).ShouldBe(new[] { (11, "Scales"), (12, "Arpeggios") });
            dtos[0].Status.ShouldBe("Doing");
            dtos[0].Priority.ShouldBe("High");
        }

        [Fact]
        public void SearchResults_WithoutStatusOrPriority_HaveNullNames()
        {
            var dto = MappingUnderTest.ToBoardItemSearchDtos(new[] { BareItem() }).Single();

            dto.Status.ShouldBeNull();
            dto.Priority.ShouldBeNull();
        }

        [Fact]
        public void SearchResults_WithStatusAndPriorityLinksButNoLoadedEntities_HaveNullNames()
        {
            var item = BareItem();
            item.BoardItemStatus = new BoardItemStatus { StatusId = 2 };
            item.BoardItemPriority = new BoardItemPriority { PriorityId = 4 };

            var dto = MappingUnderTest.ToBoardItemSearchDtos(new[] { item }).Single();

            dto.Status.ShouldBeNull();
            dto.Priority.ShouldBeNull();
        }

        // ── Tag / Status / Priority ─────────────────────────────────────────────────

        [Fact]
        public void Taxonomy_MapsIdAndNameInOrder()
        {
            MappingUnderTest.ToTagDtos(new[] { new Tag { Id = 1, Name = "Practice" }, new Tag { Id = 2, Name = "Recital" } })
                .Select(t => (t.Id, t.Name)).ShouldBe(new[] { (1, "Practice"), (2, "Recital") });

            MappingUnderTest.ToStatusDtos(new[] { new Status { Id = 3, Name = "Done" } })
                .Select(s => (s.Id, s.Name)).ShouldBe(new[] { (3, "Done") });

            MappingUnderTest.ToPriorityDtos(new[] { new Priority { Id = 5, Name = "Low" } })
                .Select(p => (p.Id, p.Name)).ShouldBe(new[] { (5, "Low") });
        }
    }
}
