using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class StatusService : IStatusService
{
    private readonly IStatusRepository _statusRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public StatusService(
        IStatusRepository repository,
        IHttpContextAccessor httpContextAccessor,
        UserManager<IdentityUser> userManager)
    {
        _statusRepository = repository;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;

    }

    public async Task<Result<List<StatusDTO>>> GetAll()
    {
        List<Status> statuses = await _statusRepository.GetAll();
        if (statuses is null)
            return Result<List<StatusDTO>>.Failure(null, "no statuses found");

        try
        {
            List<StatusDTO> statusDTOs = new List<StatusDTO>();
            foreach (Status s in statuses)
            {
                statusDTOs.Add(new StatusDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                });
            }

            return Result<List<StatusDTO>>.Success(statusDTOs);
        }
        catch (Exception ex)
        {
            return Result<List<StatusDTO>>.Failure(null, $"unable to get statuses{ex.InnerException.Message}");
        }
    }

    public async Task<Result<StatusDTO>> Insert(StatusDTO data)
    {
        if (data is null)
            return Result<StatusDTO>.Failure(data, "the status dto is null");

        IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        Status status = new Status
        {
            Name = data.Name,
            Code = data.Code,
            CreatedOn = DateTime.Now,
            CreatedBy = currentUser.Id,
        };
        try
        {
            await _statusRepository.Insert(status);
            return Result<StatusDTO>.Success(data);

        }
        catch (Exception ex)
        {
            return Result<StatusDTO>.Failure(data, $"unable to insert a status: {ex.InnerException.Message}");
        }
    }

    public async Task<int> GetStatusIdByName(string statusName)
    {
        try
        {
            Status status = await _statusRepository.GetStatusByNameAsync(statusName);

            if (status != null)
                return status.Id;
            else
                return 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting status ID by name: {ex.Message}", ex);
        }
    }
    public async Task<Result<StatusDTO>> GetById(int id)
    {
        try
        {
            Status status = await _statusRepository.GetById(id);
            if (status != null)
            {
                StatusDTO statusDTO = new StatusDTO
                {
                    Id = status.Id,
                    Name = status.Name,
                    Code = status.Code,
                };

                return Result<StatusDTO>.Success(statusDTO);
            }
            else
            {
                return Result<StatusDTO>.Failure(null, "no statuse found");
            }
        }
        catch (Exception ex)
        {
            return Result<StatusDTO>.Failure(null, $"unable to get status{ex.InnerException.Message}");
        }
    }
}