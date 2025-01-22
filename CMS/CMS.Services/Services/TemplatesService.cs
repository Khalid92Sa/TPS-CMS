using CMS.Application.DTOs;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class TemplatesService : ITemplatesService
{
    private readonly ITemplatesRepository _templatesRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TemplatesService(ITemplatesRepository templatesRepository) => _templatesRepository = templatesRepository;

    public async Task<IEnumerable<TemplatesDTO>> GetAllTemplatesAsync()
    {
        IEnumerable<Templates> templates = await _templatesRepository.GetAllTemplates();
        
        return templates.Select(i => new TemplatesDTO
        {
            TemplatesId = i.TemplatesId,
            Title = i.Title,
            BodyDesc = i.BodyDesc,
            Name = i.Name.ToString(),
        });
    }

    public async Task<TemplatesDTO> GetTemplateByIdAsync(int templatesId)
    {
        Templates templates = await _templatesRepository.GetTemplateById(templatesId);
        
        if (templates is null)
            return null;

        return new TemplatesDTO
        {
            TemplatesId = templates.TemplatesId,
            Title = templates.Title,
            BodyDesc = templates.BodyDesc,
            Name = templates.Name.ToString(),
        };
    }

    public async Task Create(TemplatesDTO entity)
    {
        IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        
        if (Enum.TryParse(entity.Name, out TemplatesName name))
        {
            Templates template = new Templates
            {
                Title = entity.Title,
                BodyDesc = entity.BodyDesc,
                Name = name,
                CreatedBy = currentUser.Id,
                CreatedOn = DateTime.Now,
            };

            await _templatesRepository.Create(template);
        }
    }

    public async Task Update(int templatesId, TemplatesDTO entity)
    {
        IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        
        if (Enum.TryParse(entity.Name, out TemplatesName name))
        {
            Templates existingTemplate = await _templatesRepository.GetTemplateById(templatesId);
            
            if (existingTemplate == null)
                throw new Exception("Templates not found");

            existingTemplate.Title = entity.Title;
            existingTemplate.BodyDesc = entity.BodyDesc;
            existingTemplate.Name = name;
            existingTemplate.ModifiedOn = DateTime.Now;
            existingTemplate.ModifiedBy = currentUser.Id;

            await _templatesRepository.Update(existingTemplate);
        }
    }


    public async Task Delete(int templatesId)
    {
        Templates templates = await _templatesRepository.GetTemplateById(templatesId);
        if (templates != null)
            await _templatesRepository.Delete(templates);
    }
}