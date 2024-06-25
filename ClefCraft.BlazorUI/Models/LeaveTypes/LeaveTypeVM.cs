using System.ComponentModel.DataAnnotations;

namespace ClefCraft.BlazorUI.Models.LeaveTypes
{
    public class LeaveTypeVM //View Model
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Default Number of Days")]
        public int DefaultDays { get; set; }
    }
}
