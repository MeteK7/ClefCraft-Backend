using AutoMapper;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
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
            CreateMap<BoardItem, BoardItemDto>().ReverseMap();
        }
    }
}
