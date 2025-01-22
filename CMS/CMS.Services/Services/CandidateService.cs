using CMS.Application.DTOs;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class CandidateService : ICandidateService
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IAttachmentService _attachmentService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<IdentityUser> _userManager;

    public CandidateService(
        ICandidateRepository candidateRepository,
        IAttachmentService attachmentService,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _candidateRepository = candidateRepository;
        _attachmentService = attachmentService;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }


    public async Task<IEnumerable<CandidateDTO>> GetAllCandidatesAsync()
    {
        try
        {
            IEnumerable<Candidate> candidates = await _candidateRepository.GetAllCandidatesAsync();
            IEnumerable<CandidateDTO> data = candidates.Select(c => new CandidateDTO
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                PositionId = c.PositionId,
                PositionName = c.Position.Name,
                Name = c.Position.Name,
                TrackId = c.TrackId,
                TrackName = c.Track.Name,
                CompanyId = c.CompanyId,
                CompanyName = c.Company.Name,
                Experience = c.Experience,
                CVAttachmentId = c.CVAttachmentId,
                CreatedOn = c.CreatedOn,
            });
            return data;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<CandidateDTO> GetCandidateByIdAsync(int id)
    {
        try
        {
            Candidate candidate = await _candidateRepository.GetCandidateByIdAsync(id);

            if (candidate is null)
                return null;

            return new CandidateDTO
            {
                Id = candidate.Id,
                FullName = candidate.FullName,
                Phone = candidate.Phone,
                PositionId = candidate.PositionId,
                PositionName = candidate.Position.Name,
                Name = candidate.Position.Name,
                TrackId = candidate.TrackId,
                TrackName = candidate.Track.Name,
                CompanyId = candidate.CompanyId,
                CompanyName = candidate.Company.Name,
                Experience = candidate.Experience,
                CVAttachmentId = candidate.CVAttachmentId,
                CreatedOn = candidate.CreatedOn,
            };
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateCandidateAsync(CandidateCreateDTO candidateDTO)
    {
        try
        {
            if (candidateDTO.FileData != null)
            {
                int attachmentId = await _attachmentService.CreateAttachmentAsync(candidateDTO.FileName, candidateDTO.FileSize, candidateDTO.FileData);
                candidateDTO.CVAttachmentId = attachmentId;
            }

            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            Candidate candidate = new Candidate
            {
                FullName = candidateDTO.FullName,
                Phone = candidateDTO.Phone,
                PositionId = candidateDTO.PositionId,
                CompanyId = candidateDTO.CompanyId,
                Experience = candidateDTO.Experience,
                CVAttachmentId = candidateDTO.CVAttachmentId,
                CreatedBy = currentUser.Id,
                CreatedOn = DateTime.Now,
                TrackId = candidateDTO.TrackId,
            };
            await _candidateRepository.CreateCandidateAsync(candidate);
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateCandidateAsync(int id, CandidateDTO candidateDTO)
    {
        try
        {
            Candidate existingCandidate = await _candidateRepository.GetCandidateByIdAsync(id);

            if (existingCandidate is null)
                throw new Exception("Candidate not found");

            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            existingCandidate.FullName = candidateDTO.FullName;
            existingCandidate.Phone = candidateDTO.Phone;
            existingCandidate.PositionId = candidateDTO.PositionId;
            existingCandidate.TrackId = candidateDTO.TrackId;
            existingCandidate.CompanyId = candidateDTO.CompanyId;
            existingCandidate.Experience = candidateDTO.Experience;
            existingCandidate.CVAttachmentId = candidateDTO.CVAttachmentId;
            existingCandidate.ModifiedOn = DateTime.Now;
            existingCandidate.ModifiedBy = currentUser.Id;

            await _candidateRepository.UpdateCandidateAsync(existingCandidate);
        }

        catch (Exception)
        {
            throw;
        }
    }


    public async Task DeleteCandidateAsync(int id)
    {
        try
        {
            Candidate candidate = await _candidateRepository.GetCandidateByIdAsync(id);
            if (candidate != null)
            {
                int? attachmentToRemove = (int?)candidate.CVAttachmentId;
                await _candidateRepository.DeleteCandidateAsync(candidate);

                if (attachmentToRemove.HasValue)
                    await _attachmentService.DeleteAttachmentAsync(attachmentToRemove.Value);
            }
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateCandidateCVAsync(int id, string fileName, long fileSize, Stream fileStream)
    {
        try
        {
            Candidate candidate = await _candidateRepository.GetCandidateByIdAsync(id);
            int attachmentId = await _attachmentService.CreateAttachmentAsync(fileName, fileSize, fileStream);

            int attachmentToRemove = 0;
            
            if (candidate.CVAttachmentId != null)
                attachmentToRemove = (int)candidate.CVAttachmentId;

            candidate.CVAttachmentId = attachmentId;
            
            await _candidateRepository.UpdateCandidateAsync(candidate);
            
            if (attachmentToRemove != 0)
                await _attachmentService.DeleteAttachmentAsync(attachmentToRemove);
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int?> GetCVAttachmentIdByCandidateId(int candidateId)
    {
        try
        {
            return await _candidateRepository.GetCVAttachmentIdByCandidateId(candidateId);
        }
        catch (Exception)
        {
            throw;
        }
    }
}