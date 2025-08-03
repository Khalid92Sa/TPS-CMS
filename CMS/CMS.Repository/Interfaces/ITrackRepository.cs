using CMS.Application.DTOs;
using CMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces;

public interface ITrackRepository
{
    Task<List<Track>> GetAll();
    Task<List<TrackDTO>> GetAllWithCandidateCountAsync();
    Task<Track> GetById(int? id);
    Task<Track> Create(Track track);
    Task<bool> Update(Track track);
    Task<bool> Delete(int id);
}
