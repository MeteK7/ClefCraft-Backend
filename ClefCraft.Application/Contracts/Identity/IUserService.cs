using ClefCraft.Application.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Identity
{
    public interface IUserService
    {

        Task<List<Assignee>> GetAssignees();
        Task<Assignee> GetAssignee(string userId);
        Task<List<User>> GetEmployees();
        Task<User> GetUser(string userId);
        Task<List<User>> GetUsersByIds(List<string> userIds);
        public string UserId { get; }
    }
}
