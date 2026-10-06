using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class DelegationDurationService : IDelegationDurationService
{
    private readonly IDelegationDurationRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DelegationDurationService(
        IDelegationDurationRepository repository,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<List<DelegationDurationDTO>>> GetAllAsync()
    {
        try
        {
            List<DelegationDuration> durations = await _repository.GetAllAsync();
            return Result<List<DelegationDurationDTO>>.Success(durations.Select(Map).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<DelegationDurationDTO>>.Failure(null, $"Unable to load delegation durations: {ex.Message}");
        }
    }

    public async Task<Result<DelegationDurationDTO>> GetByIdAsync(int id)
    {
        DelegationDuration duration = await _repository.GetByIdAsync(id);
        if (duration == null)
            return Result<DelegationDurationDTO>.Failure(null, "Delegation duration was not found.");

        return Result<DelegationDurationDTO>.Success(Map(duration));
    }

    public async Task<Result<DelegationDurationDTO>> CreateAsync(DelegationDurationDTO dto)
    {
        string error = await ValidateAsync(dto, null);
        if (error != null)
            return Result<DelegationDurationDTO>.Failure(null, error);

        string userId = await CurrentUserIdAsync();
        DateTime now = DateTime.Now;
        DelegationDuration duration = new()
        {
            Name = dto.Name.Trim(),
            Amount = dto.Amount,
            Unit = dto.Unit,
            DurationMinutes = ToMinutes(dto.Amount, dto.Unit),
            IsActive = dto.IsActive,
            IsDelete = false,
            CreatedBy = userId,
            CreatedOn = now,
            ModifiedBy = userId,
            ModifiedOn = now
        };

        await _repository.AddAsync(duration);
        return Result<DelegationDurationDTO>.Success(Map(duration));
    }

    public async Task<Result<DelegationDurationDTO>> UpdateAsync(DelegationDurationDTO dto)
    {
        DelegationDuration existing = await _repository.GetByIdAsync(dto.Id);
        if (existing == null)
            return Result<DelegationDurationDTO>.Failure(null, "Delegation duration was not found.");

        string error = await ValidateAsync(dto, dto.Id);
        if (error != null)
            return Result<DelegationDurationDTO>.Failure(null, error);

        existing.Name = dto.Name.Trim();
        existing.Amount = dto.Amount;
        existing.Unit = dto.Unit;
        existing.DurationMinutes = ToMinutes(dto.Amount, dto.Unit);
        existing.IsActive = dto.IsActive;
        existing.ModifiedBy = await CurrentUserIdAsync();
        existing.ModifiedOn = DateTime.Now;
        await _repository.UpdateAsync(existing);
        return Result<DelegationDurationDTO>.Success(Map(existing));
    }

    public async Task<Result<bool>> SetActiveAsync(int id, bool isActive)
    {
        DelegationDuration existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Result<bool>.Failure(false, "Delegation duration was not found.");

        existing.IsActive = isActive;
        existing.ModifiedBy = await CurrentUserIdAsync();
        existing.ModifiedOn = DateTime.Now;
        await _repository.UpdateAsync(existing);
        return Result<bool>.Success(true);
    }

    private async Task<string> ValidateAsync(DelegationDurationDTO dto, int? excludeId)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return "Name is required.";

        if (dto.Amount < 1)
            return "Enter a duration greater than zero.";

        if (ToMinutes(dto.Amount, dto.Unit) <= 0)
            return "Select a valid duration unit.";

        if (await _repository.NameExistsAsync(dto.Name, excludeId))
            return "A delegation duration with this name already exists.";

        return null;
    }

    private async Task<string> CurrentUserIdAsync()
    {
        if (_httpContextAccessor.HttpContext?.User == null)
            return null;

        IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        return user?.Id;
    }

    public static int ToMinutes(int amount, string unit)
    {
        if (amount < 1 || string.IsNullOrWhiteSpace(unit))
            return 0;

        if (string.Equals(unit, DelegationDuration.UnitMinutes, StringComparison.OrdinalIgnoreCase))
            return amount;

        if (string.Equals(unit, DelegationDuration.UnitHours, StringComparison.OrdinalIgnoreCase))
            return amount * 60;

        if (string.Equals(unit, DelegationDuration.UnitDays, StringComparison.OrdinalIgnoreCase))
            return amount * 24 * 60;

        if (string.Equals(unit, DelegationDuration.UnitWeeks, StringComparison.OrdinalIgnoreCase))
            return amount * 7 * 24 * 60;

        return 0;
    }

    private static DelegationDurationDTO Map(DelegationDuration duration)
    {
        return new DelegationDurationDTO
        {
            Id = duration.Id,
            Name = duration.Name,
            Amount = duration.Amount,
            Unit = duration.Unit,
            DurationMinutes = duration.DurationMinutes,
            IsActive = duration.IsActive
        };
    }
}
