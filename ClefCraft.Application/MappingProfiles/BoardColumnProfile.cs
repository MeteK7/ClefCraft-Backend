using AutoMapper;
using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.MappingProfiles
{
    public class BoardColumnProfile : Profile
    {
        public BoardColumnProfile()
        {
            // Mapping between BoardColumn and BoardColumnDto
            CreateMap<BoardColumn, BoardColumnDto>().ReverseMap();
        }
    }
}
