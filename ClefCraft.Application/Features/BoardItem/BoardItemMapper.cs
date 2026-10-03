using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using ClefCraft.Application.Features.Priority.Queries.GetPriorities;
using ClefCraft.Application.Features.Status.Queries.GetStatuses;
using ClefCraft.Application.Features.Tag.Queries.GetTags;

namespace ClefCraft.Application.Features.BoardItem
{
    public static class BoardItemMapper
    {
        public static BoardItemDto ToDto(Domain.BoardItem item)
        {
            return new BoardItemDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description!,
                BoardId = item.BoardId,
                BoardColumnId = item.BoardColumnId,
                Status = ToDtoOrNull(item.BoardItemStatus?.Status)!,
                Priority = ToDtoOrNull(item.BoardItemPriority?.Priority)!,
                Tags = item.BoardItemTags?.Select(bt => ToDtoOrNull(bt.Tag)!).ToList() ?? new(),
                AssigneeId = item.AssigneeId!,
                DueDate = item.DueDate,
                EstimatedTime = item.EstimatedTime,
                TimeSpent = item.TimeSpent,
                CreatedBy = item.CreatedBy!,
                ModifiedBy = item.ModifiedBy!,
                DateCreated = item.DateCreated ?? default,
                DateModified = item.DateModified ?? default
                // Assignee and author names come from the user service; the handlers fill them.
            };
        }

        /// <summary>Status, priority, tags and assignee names are filled by the handlers.</summary>
        public static BoardItemByIdDto ToByIdDto(Domain.BoardItem item)
        {
            return new BoardItemByIdDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description!,
                AssigneeId = item.AssigneeId!,
                DueDate = item.DueDate ?? default,
                EstimatedTime = item.EstimatedTime ?? 0,
                TimeSpent = item.TimeSpent ?? 0
            };
        }

        /// <summary>RelationId is filled by the handler.</summary>
        public static RelationshipCardDto ToRelationshipCard(Domain.BoardItem item)
        {
            return new RelationshipCardDto
            {
                ItemId = item.Id,
                Title = item.Title,
                // No status link means "": a link whose Status isn't loaded means null.
                Status = item.BoardItemStatus != null ? item.BoardItemStatus.Status?.Name! : "",
                Priority = item.BoardItemPriority != null ? item.BoardItemPriority.Priority?.Name! : "",
                AssigneeId = item.AssigneeId,
                DueDate = item.DueDate
            };
        }

        public static BoardItemSearchDto ToSearchDto(Domain.BoardItem item)
        {
            return new BoardItemSearchDto
            {
                Id = item.Id,
                Title = item.Title,
                Status = item.BoardItemStatus?.Status?.Name!,
                Priority = item.BoardItemPriority?.Priority?.Name!
            };
        }

        public static TagDto ToDto(Domain.Tag tag) => new() { Id = tag.Id, Name = tag.Name };

        public static StatusDto ToDto(Domain.Status status) => new() { Id = status.Id, Name = status.Name };

        public static PriorityDto ToDto(Domain.Priority priority) => new() { Id = priority.Id, Name = priority.Name };

        private static TagDto? ToDtoOrNull(Domain.Tag? tag) => tag is null ? null : ToDto(tag);

        private static StatusDto? ToDtoOrNull(Domain.Status? status) => status is null ? null : ToDto(status);

        private static PriorityDto? ToDtoOrNull(Domain.Priority? priority) => priority is null ? null : ToDto(priority);
    }
}
