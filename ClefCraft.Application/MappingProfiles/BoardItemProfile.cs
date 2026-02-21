using AutoMapper;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.Features.Priority.Queries.GetPriorities;
using ClefCraft.Application.Features.Status.Queries.GetStatuses;
using ClefCraft.Application.Features.Tag.Queries.GetTags;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.MappingProfiles
{
    public class BoardItemProfile : Profile
    {
        public BoardItemProfile()
        {
            // Mapping between BoardItem and BoardItemDto
            CreateMap<BoardItem, BoardItemDto>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src =>
                        src.BoardItemStatus != null
                            ? src.BoardItemStatus.Status
                            : null))

                .ForMember(dest => dest.Priority,
                    opt => opt.MapFrom(src =>
                        src.BoardItemPriority != null
                            ? src.BoardItemPriority.Priority
                            : null))

                .ForMember(dest => dest.Tags,
                    opt => opt.MapFrom(src =>
                        src.BoardItemTags.Select(bt => bt.Tag).ToList()
                    ))

                .ForMember(dest => dest.AssigneeId,
                    opt => opt.MapFrom(src => src.AssigneeId));

            CreateMap<BoardItem, BoardItemByIdDto>();
            CreateMap<Tag, TagDto>().ReverseMap();
            CreateMap<Status, StatusDto>().ReverseMap();
            CreateMap<Priority, PriorityDto>().ReverseMap();
        }
    }
}
