using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace CMS.Services.Services;

public class PositionService : IPositionService
{
    private readonly IPositionRepository _positionRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAttachmentService _attachmentService;
    public PositionService(
        IPositionRepository repository,
        IHttpContextAccessor httpContextAccessor,
        UserManager<IdentityUser> userManager,
        IAttachmentService attachmentService)
    {
        _positionRepository = repository;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _attachmentService = attachmentService;
    }


    public async Task<Result<PositionDTO>> Delete(int id)
    {
        try
        {
            Position position = await _positionRepository.GetById(id);

            if (position != null)
            {
                int attachmentToRemove = 0;

                if (position.EvaluationId != null)
                    attachmentToRemove = (int)position.EvaluationId;

                await _positionRepository.Delete(id)
;
                if (attachmentToRemove != 0)
                    await _attachmentService.DeleteAttachmentAsync(attachmentToRemove);
            }

            return Result<PositionDTO>.Success(null);
        }
        catch (Exception ex)
        {
            return Result<PositionDTO>.Failure(null, $"An error occurred while deleting the position {ex.InnerException.Message}");
        }
    }

    public async Task<Result<IEnumerable<PositionDTO>>> GetAll()
    {
        List<Position> positions = await _positionRepository.GetAll();

        if (positions == null)
            return Result<IEnumerable<PositionDTO>>.Failure(null, "no positions found");

        try
        {
            List<PositionDTO> positionDTOS = [];
            foreach (Position position in positions)
            {
                positionDTOS.Add(new PositionDTO
                {
                    Id = position.Id,
                    Name = position.Name,
                    EvaluationId = position.EvaluationId,
                    CreatedOn = position.CreatedOn
                });
            }

            return Result<IEnumerable<PositionDTO>>.Success(positionDTOS);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PositionDTO>>.Failure(null, $"unable to get positions{ex.InnerException.Message}");
        }
    }

    public async Task<Result<PositionDTO>> GetById(int id)
    {
        if (id <= 0)
            return Result<PositionDTO>.Failure(null, "Invalid position id");

        try
        {
            Position position = await _positionRepository.GetById(id);
            PositionDTO positionDTO = new PositionDTO
            {
                Id = position.Id,
                Name = position.Name,
                EvaluationId = position.EvaluationId,
                CreatedOn = position.CreatedOn,
            };

            return Result<PositionDTO>.Success(positionDTO);
        }
        catch (Exception ex)
        {
            return Result<PositionDTO>.Failure(null, $"unable to retrieve the position from the repository{ex.InnerException.Message}");
        }
    }

    public async Task<Result<PositionDTO>> Insert(PositionDTO data)
    {
        if (data is null)
            return Result<PositionDTO>.Failure(data, "the position DTO is null");

        if (data.FileData != null)
        {
            int attachmentId = await _attachmentService.CreateAttachmentAsync(data.FileName, (long)data.FileSize, data.FileData);
            data.EvaluationId = attachmentId;
        }

        IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

        try
        {
            Position position = new()
            {
                Name = data.Name,
                CreatedBy = currentUser.Id,
                CreatedOn = DateTime.Now,
                EvaluationId = data.EvaluationId,
            };

            await _positionRepository.Insert(position);
            return Result<PositionDTO>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<PositionDTO>.Failure(data, $"unable to insert a position: {ex.InnerException.Message}");
        }
    }

    public async Task<Result<PositionDTO>> Update(PositionDTO data)
    {
        try
        {
            if (data is null)
                return Result<PositionDTO>.Failure(null, "can not update a null object");

            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            Position previouePos = await _positionRepository.GetById(data.Id);

            Position position = new Position
            {
                Id = data.Id,
                Name = data.Name,
                EvaluationId = data.EvaluationId,
                ModifiedBy = currentUser.Id,
                ModifiedOn = DateTime.Now,
                CreatedBy = previouePos.CreatedBy,
                CreatedOn = previouePos.CreatedOn,
            };

            await _positionRepository.Update(position);
            return Result<PositionDTO>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<PositionDTO>.Failure(data, $"error updating the position {ex.InnerException.Message}");
        }
    }
    public async Task UpdatePositionEvaluationAsync(int id, string fileName, long fileSize, Stream fileStream)
    {
        try
        {
            Position position = await _positionRepository.GetById(id);
            int attachmentId = await _attachmentService.CreateAttachmentAsync(fileName, fileSize, fileStream);
            int attachmentToRemove = 0;

            if (position.EvaluationId != null)
                attachmentToRemove = (int)position.EvaluationId;

            position.EvaluationId = attachmentId;

            await _positionRepository.Update(position);

            if (attachmentToRemove != 0)
                await _attachmentService.DeleteAttachmentAsync(attachmentToRemove);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public bool DoesPositionNameExist(string name)
    {
        try
        {
            return _positionRepository.DoesPositionNameExist(name);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}