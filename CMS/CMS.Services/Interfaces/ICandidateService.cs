using CMS.Application.DTOs;
using CMS.Application.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface ICandidateService
{
    Task<IEnumerable<CandidateDTO>> GetAllCandidatesAsync();
    Task<PaginatedList<CandidateDTO>> GetCandidatesPageAsync(string fullName, string phone, int? trackId, int pageNumber, int pageSize);
    Task<CandidateDTO> GetCandidateByIdAsync(int id);
    Task CreateCandidateAsync(CandidateCreateDTO candidateDTO);
    Task UpdateCandidateAsync(int id, CandidateDTO candidateDTO);
    Task UpdateCandidateCVAsync(int id, string fileName, long fileSize, Stream fileStream);
    Task DeleteCandidateAsync(int id);
    Task<int?> GetCVAttachmentIdByCandidateId(int candidateId);
}
