using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Services
{
    public class TrackService : ITrackService
    {
        public ITrackRepository _trackRepository { get; }

        public TrackService(ITrackRepository trackRepository) => _trackRepository = trackRepository;

        public async Task<Result<List<TrackDTO>>> GetAll()
        {
            var trackDTOs = await _trackRepository.GetAllWithCandidateCountAsync();

            if (trackDTOs == null || trackDTOs.Count == 0)
                return Result<List<TrackDTO>>.Failure(null, MessageCode.NotFound.GetDescription());

            try
            {
                return Result<List<TrackDTO>>.Success(trackDTOs);
            }
            catch (Exception ex)
            {
                return Result<List<TrackDTO>>.Failure(null, $"Unable to get tracks: {ex.Message}");
            }
        }

        public async Task<Result<TrackDTO>> GetById(int id)
        {
            try
            {
                Track track = await _trackRepository.GetById(id);

                if (track != null)
                {
                    var trackDTO = new TrackDTO
                    {
                        Id = track.Id,
                        Name = track.Name,
                    };

                    return Result<TrackDTO>.Success(trackDTO);
                }
                else
                {
                    return Result<TrackDTO>.Failure(null, MessageCode.NotFound.GetDescription());
                }
            }
            catch (Exception ex)
            {
                return Result<TrackDTO>.Failure(null, $"Unable to get track: {ex.Message}");
            }
        }

        public async Task<Result<TrackDTO>> Create(TrackDTO dto)
        {
            try
            {
                Track track = new()
                {
                    Name = dto.Name,
                };

                var result = await _trackRepository.Create(track);

                dto.Id = result.Id;

                return Result<TrackDTO>.Success(dto, MessageCode.Success.GetDescription());
            }
            catch (Exception ex)
            {
                return Result<TrackDTO>.Failure(null, $"Unable to create track: {ex.Message}");
            }
        }

        public async Task<Result<bool>> Update(TrackDTO dto)
        {
            try
            {
                Track existing = await _trackRepository.GetById(dto.Id);

                if (existing == null)
                    return Result<bool>.Failure(false, MessageCode.NotFound.GetDescription());

                existing.Name = dto.Name;

                bool result = await _trackRepository.Update(existing);

                return result
                    ? Result<bool>.Success(true, MessageCode.Success.GetDescription())
                    : Result<bool>.Failure(false, MessageCode.UpdateFailed.GetDescription());
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(false, $"Unable to update track: {ex.Message}");
            }
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {
                bool result = await _trackRepository.Delete(id);

                return result
                    ? Result<bool>.Success(true, MessageCode.Success.GetDescription())
                    : Result<bool>.Failure(false, MessageCode.NotFound.GetDescription());
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(false, MessageCode.DeleteFailed.GetDescription());
            }
        }
    }
}
