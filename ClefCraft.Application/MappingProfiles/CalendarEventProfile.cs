using AutoMapper;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.MappingProfiles
{
    public class CalendarEventProfile : Profile
    {
        public CalendarEventProfile()
        {
            CreateMap<CalendarEvent, CalendarEventDto>().ReverseMap();
            CreateMap<CalendarEvent, WorkHistoryDto>().ReverseMap();
            CreateMap<CalendarEventAttachment, CalendarEventAttachmentDto>().ReverseMap();
            CreateMap<EventType, EventTypeDto>().ReverseMap();

        }
    }
}
