using AutoMapper;
using ClefCraft.Application.Features.Board.Queries.GetBoards;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.MappingProfiles
{
    public class BoardProfile:Profile
    {
        public BoardProfile()
        {
            CreateMap<Board, BoardDto>().ReverseMap();
        }
    }
}
