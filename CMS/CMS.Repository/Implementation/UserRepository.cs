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
}