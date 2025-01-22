using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class AttachmentRepository : IAttachmentRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AttachmentRepository(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<IEnumerable<Attachment>> GetAllAttachmentsAsync()
    {
        try
        {
            return await _dbContext.Attachments.ToListAsync();
        }
        catch (Exception ex)
        {
            throw new ProblemDetailsException(new ProblemDetails
            {
                Title = "Error retrieving attachments",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
    public async Task<Attachment> GetAttachmentByIdAsync(int id)
    {
        try
        {
            return await _dbContext.Attachments.FindAsync(id);
        }
        catch (Exception ex)
        {
            throw new ProblemDetailsException(new ProblemDetails
            {
                Title = "Error retrieving attachment",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    public async Task<int> CreateAttachmentAsync(Attachment attachment)
    {
        try
        {
            await _dbContext.Attachments.AddAsync(attachment);
            await _dbContext.SaveChangesAsync();
            return attachment.Id;
        }
        catch (Exception ex)
        {
            throw new ProblemDetailsException(new ProblemDetails
            {
                Title = "Error creating attachment",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
    public async Task DeleteAttachmentAsync(int id)
    {
        try
        {
            Attachment attachment = await _dbContext.Attachments.FindAsync(id);
            if (attachment != null)
            {
                _dbContext.Attachments.Remove(attachment);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new ProblemDetailsException(new ProblemDetails
            {
                Title = "Error deleting attachment",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}