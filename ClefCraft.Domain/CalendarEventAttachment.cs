using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class CalendarEventAttachment : BaseEntity
    {
        public int CalendarEventId { get; set; }
        public CalendarEvent CalendarEvent { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string StoredFilePath { get; set; } = null!;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = null!;
    }
}
