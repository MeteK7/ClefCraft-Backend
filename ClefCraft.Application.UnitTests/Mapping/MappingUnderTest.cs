using AutoMapper;
using ClefCraft.Application.Features.Board.Queries.GetBoards;
using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Application.Features.Priority.Queries.GetPriorities;
using ClefCraft.Application.Features.Status.Queries.GetStatuses;
using ClefCraft.Application.Features.Tag.Queries.GetTags;
using ClefCraft.Application.MappingProfiles;
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
        private static readonly IMapper Mapper =
            new MapperConfiguration(cfg => cfg.AddMaps(typeof(BoardProfile).Assembly)).CreateMapper();

        public static BoardDto ToBoardDto(Board board) => Mapper.Map<BoardDto>(board);
        public static List<BoardDto> ToBoardDtos(IEnumerable<Board> boards) => Mapper.Map<List<BoardDto>>(boards);

        public static BoardColumnDto ToBoardColumnDto(BoardColumn column) => Mapper.Map<BoardColumnDto>(column);
        public static List<BoardColumnDto> ToBoardColumnDtos(IEnumerable<BoardColumn> columns) => Mapper.Map<List<BoardColumnDto>>(columns);

        public static BoardItemDto ToBoardItemDto(BoardItem item) => Mapper.Map<BoardItemDto>(item);
        public static BoardItemByIdDto ToBoardItemByIdDto(BoardItem item) => Mapper.Map<BoardItemByIdDto>(item);
        public static RelationshipCardDto ToRelationshipCardDto(BoardItem item) => Mapper.Map<RelationshipCardDto>(item);
        public static List<BoardItemSearchDto> ToBoardItemSearchDtos(IEnumerable<BoardItem> items) => Mapper.Map<List<BoardItemSearchDto>>(items);

        public static List<TagDto> ToTagDtos(IEnumerable<Tag> tags) => Mapper.Map<List<TagDto>>(tags);
        public static List<StatusDto> ToStatusDtos(IEnumerable<Status> statuses) => Mapper.Map<List<StatusDto>>(statuses);
        public static List<PriorityDto> ToPriorityDtos(IEnumerable<Priority> priorities) => Mapper.Map<List<PriorityDto>>(priorities);

        public static CalendarEventDto ToCalendarEventDto(CalendarEvent calendarEvent) => Mapper.Map<CalendarEventDto>(calendarEvent);
        public static CalendarEventDto ToCalendarEventDto(CalendarEventInstanceDto instance) => Mapper.Map<CalendarEventDto>(instance);
        public static CalendarEventAttachmentDto ToAttachmentDto(CalendarEventAttachment attachment) => Mapper.Map<CalendarEventAttachmentDto>(attachment);
        public static List<CalendarEventAttachmentDto> ToAttachmentDtos(IEnumerable<CalendarEventAttachment> attachments) => Mapper.Map<List<CalendarEventAttachmentDto>>(attachments);
        public static List<EventTypeDto> ToEventTypeDtos(IEnumerable<EventType> eventTypes) => Mapper.Map<List<EventTypeDto>>(eventTypes);
    }
}
