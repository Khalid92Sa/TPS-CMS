using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class NotificationsRepository : INotificationsRepository
{

    private readonly ApplicationDbContext Db;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string HrRoleName = "HR Manager";
    private readonly string GeneralManagerRoleName = "General Manager";
    private readonly string SolutionArchiRoleName = "Solution Architecture";

    public NotificationsRepository(
        ApplicationDbContext _db,
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        Db = _db;
        _roleManager = roleManager;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<Notifications>> GetAllNotifications()
    {
        try
        {
            return await Db.Notifications.ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Notifications> GetNotificationsById(int interviewId)
    {
        try
        {
            return await Db.Notifications.FindAsync(interviewId);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task Create(Notifications entity)
    {
        try
        {
            entity.IsActive = true;
            entity.ModifiedBy = entity.ModifiedBy;
            entity.ModifiedOn = DateTime.Now;

            await Db.Notifications.AddAsync(entity);
            await Db.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task Update(Notifications entity)
    {
        try
        {
            entity.IsActive = true;
            entity.ModifiedBy = entity.ModifiedBy;
            entity.ModifiedOn = DateTime.Now;

            Db.Notifications.Update(entity);
            await Db.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task Delete(Notifications entity)
    {
        try
        {
            entity.IsDelete = true;
            entity.ModifiedBy = entity.ModifiedBy;
            entity.ModifiedOn = DateTime.Now;

            Db.Notifications.Remove(entity);
            await Db.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Notifications>> GetSpacificNotificationsforHR()
    {
        try
        {
            string HrId = "";

            IdentityRole Hr = await _roleManager.FindByNameAsync(HrRoleName);

            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            return await Db.Notifications.Where(x => x.ReceiverId == HrId)
                                         .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Notifications>> GetSpacificNotificationsforGeneral()
    {
        try
        {
            string managerId = "";

            IdentityRole manager = await _roleManager.FindByNameAsync(GeneralManagerRoleName);

            managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;

            return await Db.Notifications.Where(x => x.ReceiverId == managerId)
                                         .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Notifications>> GetSpacificNotificationsforArchi()
    {
        try
        {
            string archiId = "";

            IdentityRole archi = await _roleManager.FindByNameAsync(SolutionArchiRoleName);

            archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

            return await Db.Notifications.Where(x => x.ReceiverId == archiId)
                                         .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Notifications>> GetSpacificNotificationsforInterviewer(string interviewerId)
    {
        try
        {
            return await Db.Notifications.Where(x => x.ReceiverId == interviewerId)
                                         .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }




}
