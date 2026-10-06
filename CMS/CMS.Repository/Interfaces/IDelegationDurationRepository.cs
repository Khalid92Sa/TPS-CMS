using CMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces;

public interface IDelegationDurationRepository
{
    Task<List<DelegationDuration>> GetAllAsync();
    Task<List<DelegationDuration>> GetActiveAsync();
    Task<DelegationDuration> GetByIdAsync(int id);
    Task<DelegationDuration> GetActiveByIdAsync(int id);
    Task<bool> NameExistsAsync(string name, int? excludeId);
    Task AddAsync(DelegationDuration duration);
    Task UpdateAsync(DelegationDuration duration);
}
