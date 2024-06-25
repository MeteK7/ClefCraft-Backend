using AutoMapper;
using ClefCraft.Application.Features.LeaveAllocation.Commands.CreateLeaveAllocation;
using ClefCraft.Application.Features.LeaveAllocation.Commands.UpdateLeaveAllocation;
using ClefCraft.Application.Features.LeaveAllocation.Queries.GetLeaveAllocationDetails;
using ClefCraft.Application.Features.LeaveAllocation.Queries.GetLeaveAllocations;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.MappingProfiles
{
    public class LeaveAllocationProfile : Profile
    {
        public LeaveAllocationProfile()
        {
            CreateMap<LeaveAllocationDto, LeaveAllocation>().ReverseMap();
            CreateMap<LeaveAllocation, LeaveAllocationDetailsDto>();
            CreateMap<CreateLeaveAllocationCommand, LeaveAllocation>();
            CreateMap<UpdateLeaveAllocationCommand, LeaveAllocation>();
        }
    }
}
