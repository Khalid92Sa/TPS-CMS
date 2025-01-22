using CMS.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface ITemplatesService
{
    Task<IEnumerable<TemplatesDTO>> GetAllTemplatesAsync();
    Task<TemplatesDTO> GetTemplateByIdAsync(int templatesId);
    Task Create(TemplatesDTO entity);
    Task Update(int templatesId, TemplatesDTO entity);
    Task Delete(int templatesId);
}
