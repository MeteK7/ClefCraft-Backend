using System.ComponentModel.DataAnnotations;

namespace ClefCraft.Api.Models
{
    /// <summary>
    /// Body of POST api/Boards/{boardId}/Members: only the user being added. The board comes from
    /// the route and the caller from the token, so neither can be supplied (or spoofed) here.
    /// </summary>
    public class AddBoardMemberRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}
