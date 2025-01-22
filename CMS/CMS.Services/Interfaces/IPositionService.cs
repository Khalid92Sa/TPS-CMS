using CMS.Application.DTOs;
using CMS.Application.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface IPositionService
{
    Task<Result<PositionDTO>> Insert(PositionDTO data);
    Task<Result<IEnumerable<PositionDTO>>> GetAll();
    Task<Result<PositionDTO>> Delete(int id);
    Task<Result<PositionDTO>> GetById(int id);
    Task<Result<PositionDTO>> Update(PositionDTO data);
    Task UpdatePositionEvaluationAsync(int id, string fileName, long fileSize, Stream fileStream);
    bool DoesPositionNameExist(string name);
}
