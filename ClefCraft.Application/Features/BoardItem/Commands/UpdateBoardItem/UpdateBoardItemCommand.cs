using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem
{
    public class UpdateBoardItemCommand : IRequest<BoardItemByIdDto>
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public List<int>? TagIds { get; set; }
        public string? AssigneeId { get; set; }
        public DateTime? DueDate { get; set; }
        public double? EstimatedTime { get; set; }
        public double? TimeSpent { get; set; }
        public int BoardColumnId { get; set; }
    }
}
