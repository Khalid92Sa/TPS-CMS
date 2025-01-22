using CMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces;

public interface ICarrerOfferRepository
{
    Task<IEnumerable<CarrerOffer>> GetAllCarrerOffersAsync();
    Task<CarrerOffer> GetCarrerOfferByIdAsync(int carrerOfferId);
    Task<int> CreateCarrerOfferAsync(CarrerOffer carrerOffer);
    Task UpdateCarrerOfferAsync(CarrerOffer carrerOffer);
    Task DeleteCarrerOfferAsync(int id);
    Task<int> CountAllAsync();
}
