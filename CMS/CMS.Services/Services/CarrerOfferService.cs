using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class CarrerOfferService : ICarrerOfferService
{
    private readonly ICarrerOfferRepository _carrerOfferRepository;
    private readonly IPositionService _positionService;

    public CarrerOfferService(
        ICarrerOfferRepository carrerOfferRepository,
        IPositionService positionService)
    {
        _carrerOfferRepository = carrerOfferRepository;
        _positionService = positionService;
    }

    public async Task<Result<int>> Delete(int id)
    {
        try
        {
            await _carrerOfferRepository.DeleteCarrerOfferAsync(id);
            return Result<int>.Success(id);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(-1, $"An error occurred while deleting the career offer{ex.InnerException.Message}");
        };
    }

    public async Task<Result<List<CarrerOfferDTO>>> GetAll()
    {
        try
        {
            IEnumerable<CarrerOffer> careerOffers = await _carrerOfferRepository.GetAllCarrerOffersAsync();
            
            if (careerOffers is null)
                return Result<List<CarrerOfferDTO>>.Failure(null, "No career offers found");

            List<CarrerOfferDTO> carrerOfferDTOs = new List<CarrerOfferDTO>();
            
            foreach (CarrerOffer c in careerOffers)
            {
                CarrerOfferDTO com = new CarrerOfferDTO
                {
                    Id = c.Id,
                    LongDescription = c.LongDescription,
                    YearsOfExperience = c.YearsOfExperience,
                    PositionId = c.PositionId,
                    PositionName = c.Position.Name,
                    CreatedBy = c.CreatedBy,
                    CreatedDateTime = c.CreatedDateTime,
                };

                carrerOfferDTOs.Add(com);
            }
            return Result<List<CarrerOfferDTO>>.Success(carrerOfferDTOs);
        }
        catch (Exception ex)
        {
            return Result<List<CarrerOfferDTO>>.Failure(null, $"Unable to get career offers: {ex.InnerException.Message}");
        }
    }

    public async Task<Result<CarrerOfferDTO>> GetById(int id)
    {
        if (id <= 0)
            return Result<CarrerOfferDTO>.Failure(null, "Invalid career offer id");
    
        try
        {
            CarrerOffer careeroffer = await _carrerOfferRepository.GetCarrerOfferByIdAsync(id);
            CarrerOfferDTO careerofferDTO = new CarrerOfferDTO
            {
                Id = careeroffer.Id,
                LongDescription = careeroffer.LongDescription,
                YearsOfExperience = careeroffer.YearsOfExperience,
                PositionId = careeroffer.PositionId,
                PositionName = careeroffer.Position.Name
            };

            return Result<CarrerOfferDTO>.Success(careerofferDTO);
        }
        catch (Exception ex)
        {
            return Result<CarrerOfferDTO>.Failure(null, $"unable to retrieve the career offer from the repository{ex.InnerException.Message}");
        }

    }

    public async Task<Result<CarrerOfferDTO>> Insert(CarrerOfferDTO data)
    {
        if (data is null)
            return Result<CarrerOfferDTO>.Failure(data, "the career offer DTO is null");

        CarrerOffer careeroffer = new CarrerOffer
        {
            PositionId = data.PositionId,
            LongDescription = data.LongDescription,
            YearsOfExperience = data.YearsOfExperience,
        };

        try
        {
            await _carrerOfferRepository.CreateCarrerOfferAsync(careeroffer);
            return Result<CarrerOfferDTO>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<CarrerOfferDTO>.Failure(data, $"unable to insert a career offer: {ex.InnerException.Message}");
        }
    }

    public async Task<Result<CarrerOfferDTO>> Update(CarrerOfferDTO data)
    {
        if (data is null)
            return Result<CarrerOfferDTO>.Failure(data, "can not update a null object");

        CarrerOffer careeroffer = new CarrerOffer
        {
            Id = data.Id,
            PositionId = data.PositionId,
            LongDescription = data.LongDescription,
            YearsOfExperience = data.YearsOfExperience,
            CreatedBy = data.CreatedBy,
        };

        try
        {
            await _carrerOfferRepository.UpdateCarrerOfferAsync(careeroffer);
            return Result<CarrerOfferDTO>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<CarrerOfferDTO>.Failure(data, $"error updating the career offer {ex.Message}");
        }
    }
}