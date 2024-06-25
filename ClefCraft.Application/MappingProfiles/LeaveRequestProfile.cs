using AutoMapper;
using ClefCraft.Application.Features.LeaveRequest.Commands.CreateLeaveRequest;
using ClefCraft.Application.Features.LeaveRequest.Commands.UpdateLeaveRequest;
using ClefCraft.Application.Features.LeaveRequest.Queries.GetLeaveRequestDetail;
using ClefCraft.Application.Features.LeaveRequest.Queries.GetLeaveRequestList;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.MappingProfiles
{
    public class LeaveRequestProfile : Profile
    {
        public LeaveRequestProfile()
        {
            CreateMap<LeaveRequestListDto, LeaveRequest>().ReverseMap();
            CreateMap<LeaveRequestDetailsDto, LeaveRequest>().ReverseMap();
            CreateMap<LeaveRequest, LeaveRequestDetailsDto>();
            CreateMap<CreateLeaveRequestCommand, LeaveRequest>();
            CreateMap<UpdateLeaveRequestCommand, LeaveRequest>();
        }
    }
}
