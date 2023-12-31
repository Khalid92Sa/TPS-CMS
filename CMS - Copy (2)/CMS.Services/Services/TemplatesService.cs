﻿using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Implementation;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Services.Services
{
    public class TemplatesService : ITemplatesService
    {
        private readonly ITemplatesRepository _templatesRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TemplatesService(ITemplatesRepository templatesRepository)
        {
            _templatesRepository = templatesRepository;
        }


        public async Task<IEnumerable<TemplatesDTO>> GetAllTemplatesAsync()
        {
            var templates = await _templatesRepository.GetAllTemplates();
            return templates.Select(i => new TemplatesDTO
            {
               TemplatesId=i.TemplatesId,
               Title=i.Title,
               BodyDesc=i.BodyDesc,
               Name=i.Name.ToString(),

            });
        }

        public async Task<TemplatesDTO> GetTemplateByIdAsync(int templatesId)
        {
            var templates = await _templatesRepository.GetTemplateById(templatesId);
            if (templates == null)
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
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            if (Enum.TryParse(entity.Name, out TemplatesName name)) 
            { 
                var template = new Templates
                {
                    Title = entity.Title,
                    BodyDesc = entity.BodyDesc,
                    Name=name,
                    CreatedBy=currentUser.Id,
                    CreatedOn=DateTime.Now,
                };
            await _templatesRepository.Create(template);
        }
        }

        public async Task Update(int templatesId, TemplatesDTO entity)
        {
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            if (Enum.TryParse(entity.Name, out TemplatesName name))
            {
                var existingTemplate = await _templatesRepository.GetTemplateById(templatesId);
                if (existingTemplate == null)
                    throw new Exception("Templates not found");

                existingTemplate.Title = entity.Title;
                existingTemplate.BodyDesc = entity.BodyDesc;
                existingTemplate.Name = name;
                existingTemplate.ModifiedOn= DateTime.Now;
                existingTemplate.ModifiedBy=currentUser.Id;

                await _templatesRepository.Update(existingTemplate);
            }
        }


        public async Task Delete(int templatesId)
        {

            var templates = await _templatesRepository.GetTemplateById(templatesId);
            if (templates != null)
                await _templatesRepository.Delete(templates);
        }
    }
}
