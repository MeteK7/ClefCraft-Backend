using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Models.Identity
{
    public class Assignee
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Firstname { get; set; } = null!;
        public string Lastname { get; set; } = null!;

        public string FullName => $"{Firstname} {Lastname}";
    }
}
