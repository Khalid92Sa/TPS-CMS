using CMS.Application.DTOs;
using CMS.Application.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface IDelegationDurationService
{
    Task<Result<List<DelegationDurationDTO>>> GetAllAsync();
    Task<Result<DelegationDurationDTO>> GetByIdAsync(int id);
    Task<Result<DelegationDurationDTO>> CreateAsync(DelegationDurationDTO dto);
    Task<Result<DelegationDurationDTO>> UpdateAsync(DelegationDurationDTO dto);
    Task<Result<bool>> SetActiveAsync(int id, bool isActive);
}
