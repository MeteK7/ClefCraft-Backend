using AutoMapper;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class GetWorkHistoryQueryHandler : IRequestHandler<GetWorkHistoryQuery, List<WorkHistoryDto>>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public GetWorkHistoryQueryHandler(ICalendarEventRepository calendarEventRepository, IMapper mapper, IUserService userService)
        {
            _calendarEventRepository = calendarEventRepository;
            _mapper = mapper;
            _userService = userService;
        }
        public async Task<List<WorkHistoryDto>> Handle(
            GetWorkHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var history = await _calendarEventRepository
                .GetWorkHistoryByItemIdAsync(request.ItemId);

            var userIds = history
                .Select(h => h.CreatedBy) // or h.UserId
                .Where(userId => !string.IsNullOrEmpty(userId))
                .Distinct()
                .ToList();

            var users = await _userService.GetUsersByIds(userIds);

            var result = history.Select(h =>
            {
                var user = users.FirstOrDefault(u => u.Id == h.CreatedBy);

                return new WorkHistoryDto
                {
                    DateCreated = h.StartDate.UtcDateTime,
                    ActionByUserId = h.CreatedBy,
                    ActionByFullName = user != null
                        ? $"{user.Firstname} {user.Lastname}"
                        : "Unknown"
                };
            }).ToList();

            return result;
        }
    }
}
