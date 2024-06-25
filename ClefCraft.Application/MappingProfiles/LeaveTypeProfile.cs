using AutoMapper;
using ClefCraft.Application.Features.LeaveType.Commands.CreateLeaveType;
using ClefCraft.Application.Features.LeaveType.Commands.UpdateLeaveType;
using ClefCraft.Application.Features.LeaveType.Queries.GetAllLeaveTypes;
using ClefCraft.Application.Features.LeaveType.Queries.GetLeaveTypeDetails;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.MappingProfiles
{
    public class LeaveTypeProfile : Profile
    {
        public LeaveTypeProfile()
        {
            CreateMap<LeaveTypeDto, LeaveType>().ReverseMap();
            CreateMap<LeaveType, LeaveTypeDetailsDto>();
            CreateMap<CreateLeaveTypeCommand, LeaveType>();
            CreateMap<UpdateLeaveTypeCommand, LeaveType>();
        }
    }
}
