using CMS.Application.DTOs;
using CMS.Domain;
using CMS.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class UserRepository : IUserRepository
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
    }

    public async Task<bool> Delete(string id)
    {
        try
        {
            IdentityUser user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return false;

            IdentityResult result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<IdentityUser>> GetAllUsersWithRoles() => await _dbContext.Users.ToListAsync();

    public List<string> GetUserRoles(IdentityUser user) => _userManager.GetRolesAsync(user).Result.ToList();

    public async Task<IdentityUser> GetUserById(string userId) => await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

    /// <summary>User names for many users in one query (user id to user name). Unknown ids are absent.</summary>
    public async Task<Dictionary<string, string>> GetUserNamesAsync(IEnumerable<string> userIds)
    {
        List<string> ids = DistinctIds(userIds);
        if (ids.Count == 0)
            return new Dictionary<string, string>();

        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);
    }

    /// <summary>
    /// The first role of each user in one query, the same role UserManager.GetRolesAsync(user)[0]
    /// returns. Users without a role are absent.
    /// </summary>
    public async Task<Dictionary<string, string>> GetFirstRoleNamesAsync(IEnumerable<string> userIds)
    {
        List<string> ids = DistinctIds(userIds);
        Dictionary<string, string> roles = new();
        if (ids.Count == 0)
            return roles;

        var rows = await (from userRole in _dbContext.UserRoles
                          join role in _dbContext.Roles on userRole.RoleId equals role.Id
                          where ids.Contains(userRole.UserId)
                          select new { userRole.UserId, role.Name })
                         .AsNoTracking()
                         .ToListAsync();

        foreach (var row in rows)
            roles.TryAdd(row.UserId, row.Name);

        return roles;
    }

    /// <summary>
    /// One page of users with their role for the Users list. Filtering, counting and paging run in
    /// SQL; the role is read in the same query instead of one GetRolesAsync call per user.
    /// </summary>
    public async Task<(List<Register> Users, int TotalCount)> GetUsersPageAsync(string userNameFilter, int pageNumber, int pageSize)
    {
        IQueryable<IdentityUser> users = _dbContext.Users.AsNoTracking();
        if (!string.IsNullOrEmpty(userNameFilter))
            users = users.Where(u => u.UserName.Contains(userNameFilter));

        int totalCount = await users.CountAsync();

        // Same order as the unordered table read used before (clustered primary key)
        List<Register> page = await users
            .OrderBy(u => u.Id)
            .Skip(Math.Max(0, (pageNumber - 1) * pageSize))
            .Take(Math.Max(0, pageSize))
            .Select(u => new Register
            {
                RegisterrId = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                SelectedRole = (from userRole in _dbContext.UserRoles
                                join role in _dbContext.Roles on userRole.RoleId equals role.Id
                                where userRole.UserId == u.Id
                                select role.Name).FirstOrDefault()
            })
            .ToListAsync();

        return (page, totalCount);
    }

    private static List<string> DistinctIds(IEnumerable<string> userIds)
    {
        return userIds?.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList() ?? [];
    }
}