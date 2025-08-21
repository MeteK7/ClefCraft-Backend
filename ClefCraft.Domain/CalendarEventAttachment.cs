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
        public CalendarEvent CalendarEvent { get; set; }
        public string FileName { get; set; }
        public string StoredFilePath { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; }
    }
}
