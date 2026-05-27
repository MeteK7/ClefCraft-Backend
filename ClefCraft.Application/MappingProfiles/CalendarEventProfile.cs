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
            CreateMap<CalendarEvent, CalendarEventDto>()
                .ForMember(
                    dest => dest.EventTypeName,
                    opt => opt.MapFrom(src =>
                        src.EventType != null
                            ? src.EventType.Name
                            : null))
                .ForMember(
                    dest => dest.EventColor,
                    opt => opt.MapFrom(src =>
                        src.EventType != null
                            ? src.EventType.Color
                            : null));

            CreateMap<CalendarEventInstanceDto, CalendarEventDto>();

            CreateMap<CalendarEventDto, CalendarEvent>()
                .ForMember(
                    dest => dest.EventType,
                    opt => opt.Ignore());

            CreateMap<CalendarEventAttachment,
                CalendarEventAttachmentDto>()
                .ReverseMap();

            CreateMap<EventType, EventTypeDto>()
                .ReverseMap();
        }
    }
}