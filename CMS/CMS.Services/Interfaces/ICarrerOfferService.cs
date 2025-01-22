using CMS.Application.DTOs;
using CMS.Application.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface ICarrerOfferService
{
    Task<Result<CarrerOfferDTO>> Insert(CarrerOfferDTO data);
    Task<Result<List<CarrerOfferDTO>>> GetAll();
    Task<Result<int>> Delete(int id);
    Task<Result<CarrerOfferDTO>> GetById(int id);
    Task<Result<CarrerOfferDTO>> Update(CarrerOfferDTO data);
}
