using CMS.Application.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces;

public interface IUserRepository
{
    Task<List<IdentityUser>> GetAllUsersWithRoles();
    List<string> GetUserRoles(IdentityUser user);
    Task<bool> Delete(string id);
    Task<IdentityUser> GetUserById(string userId);
    Task<Dictionary<string, string>> GetUserNamesAsync(IEnumerable<string> userIds);
    Task<Dictionary<string, string>> GetFirstRoleNamesAsync(IEnumerable<string> userIds);
    Task<(List<Register> Users, int TotalCount)> GetUsersPageAsync(string userNameFilter, int pageNumber, int pageSize);
}
