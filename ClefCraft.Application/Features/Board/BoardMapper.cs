using ClefCraft.Application.Features.Board.Queries.GetBoards;
using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Application.Features.BoardItem;

namespace ClefCraft.Application.Features.Board
{
    public static class BoardMapper
    {
        public static BoardDto ToDto(Domain.Board board)
        {
            return new BoardDto
            {
                Id = board.Id,
                Title = board.Title,
                OwnerUserId = board.OwnerUserId
            };
        }

        public static BoardColumnDto ToDto(Domain.BoardColumn column)
        {
            return new BoardColumnDto
            {
                Id = column.Id,
                Title = column.Title,
                BoardItems = column.BoardItems?.Select(BoardItemMapper.ToDto).ToList() ?? new()
            };
        }
    }
}
