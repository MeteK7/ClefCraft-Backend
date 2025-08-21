using AutoMapper;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
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
                .ForMember(dest => dest.Tags,
                    opt => opt.MapFrom(src =>
                        src.BoardItemTags.Select(bt => new TagDto { Id = bt.Tag.Id, Name = bt.Tag.Name }).ToList()
                        )
                    )
                .ReverseMap();
            CreateMap<BoardItem, BoardItemByIdDto>().ReverseMap();
            CreateMap<Tag, TagDto>().ReverseMap();
        }
    }
}
