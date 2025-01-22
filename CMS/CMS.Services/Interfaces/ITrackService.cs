using CMS.Application.DTOs;
using CMS.Application.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;
public interface ITrackService
{
    Task<Result<List<TrackDTO>>> GetAll();
    Task<Result<TrackDTO>> GetById(int id);
}
