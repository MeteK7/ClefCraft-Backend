using MediatR;

namespace ClefCraft.Application.Features.BoardItem.Commands.DeleteBoardItem
{
    public class DeleteBoardItemCommand : IRequest
    {
        public int Id { get; set; }
    }
}
