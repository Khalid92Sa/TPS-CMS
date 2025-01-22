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
}
