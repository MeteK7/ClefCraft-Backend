namespace ClefCraft.Application.Features.BoardMembers.DTOs
{
    public class BoardMemberDto
    {
        public int Id { get; set; }
        public int BoardId { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
    }
}
