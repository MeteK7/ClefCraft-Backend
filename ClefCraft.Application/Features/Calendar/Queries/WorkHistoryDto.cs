using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class WorkHistoryDto
    {
        public DateTime DateCreated { get; set; }
        public string ActionByUserId { get; set; }  
        public string ActionByFullName { get; set; } 
    }
}
