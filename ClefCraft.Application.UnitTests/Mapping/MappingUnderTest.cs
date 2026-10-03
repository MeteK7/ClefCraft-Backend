using ClefCraft.Application.Features.Board;
using ClefCraft.Application.Features.Board.Queries.GetBoards;
using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Application.Features.BoardItem;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using ClefCraft.Application.Features.Calendar;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Application.Features.Priority.Queries.GetPriorities;
using ClefCraft.Application.Features.Status.Queries.GetStatuses;
using ClefCraft.Application.Features.Tag.Queries.GetTags;
using ClefCraft.Domain;

namespace ClefCraft.Application.UnitTests.Mapping
{
    /// <summary>
    /// The entity-to-DTO mappings the handlers use, one method per mapping. The mapping tests
    /// call these rather than a mapping implementation directly, so the same expected values
    /// can be checked against whichever implementation the handlers use.
    /// </summary>
    internal static class MappingUnderTest
    {
        public static BoardDto ToBoardDto(Board board) => BoardMapper.ToDto(board);
        public static List<BoardDto> ToBoardDtos(IEnumerable<Board> boards) => boards.Select(BoardMapper.ToDto).ToList();

        public static BoardColumnDto ToBoardColumnDto(BoardColumn column) => BoardMapper.ToDto(column);
        public static List<BoardColumnDto> ToBoardColumnDtos(IEnumerable<BoardColumn> columns) => columns.Select(BoardMapper.ToDto).ToList();

        public static BoardItemDto ToBoardItemDto(BoardItem item) => BoardItemMapper.ToDto(item);
        public static BoardItemByIdDto ToBoardItemByIdDto(BoardItem item) => BoardItemMapper.ToByIdDto(item);
        public static RelationshipCardDto ToRelationshipCardDto(BoardItem item) => BoardItemMapper.ToRelationshipCard(item);
        public static List<BoardItemSearchDto> ToBoardItemSearchDtos(IEnumerable<BoardItem> items) => items.Select(BoardItemMapper.ToSearchDto).ToList();

        public static List<TagDto> ToTagDtos(IEnumerable<Tag> tags) => tags.Select(BoardItemMapper.ToDto).ToList();
        public static List<StatusDto> ToStatusDtos(IEnumerable<Status> statuses) => statuses.Select(BoardItemMapper.ToDto).ToList();
        public static List<PriorityDto> ToPriorityDtos(IEnumerable<Priority> priorities) => priorities.Select(BoardItemMapper.ToDto).ToList();

        public static CalendarEventDto ToCalendarEventDto(CalendarEvent calendarEvent) => CalendarMapper.ToDto(calendarEvent);
        public static CalendarEventDto ToCalendarEventDto(CalendarEventInstanceDto instance) => CalendarMapper.ToDto(instance);
        public static CalendarEventAttachmentDto ToAttachmentDto(CalendarEventAttachment attachment) => CalendarMapper.ToDto(attachment);
        public static List<CalendarEventAttachmentDto> ToAttachmentDtos(IEnumerable<CalendarEventAttachment> attachments) => attachments.Select(CalendarMapper.ToDto).ToList();
        public static List<EventTypeDto> ToEventTypeDtos(IEnumerable<EventType> eventTypes) => eventTypes.Select(CalendarMapper.ToDto).ToList();
    }
}
