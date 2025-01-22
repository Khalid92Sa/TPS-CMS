using CMS.Application.DTOs;
using CMS.Application.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface IStatusService
{
    Task<Result<StatusDTO>> Insert(StatusDTO data);
    Task<Result<List<StatusDTO>>> GetAll();
    Task<int> GetStatusIdByName(string statusName);
    Task<Result<StatusDTO>> GetById(int id);

}
