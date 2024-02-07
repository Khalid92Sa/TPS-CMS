using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation
{
    public class PositionRepository : IPositionRepository
    {
        readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PositionRepository(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public async void LogException(string methodName, Exception ex, string additionalInfo)
        {
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var userId = currentUser?.Id;
            _context.Logs.Add(new Log
            {
                MethodName = methodName,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,CreatedByUserId = userId,
                LogTime = DateTime.Now,
                AdditionalInfo = additionalInfo
            });
            _context.SaveChanges();
        }


        public async Task<int> Delete(int id)
        {
            try
            {
                //var position=await _context.Positions.FindAsync(id);

                var position =await _context.Positions.Include(c => c.Interviews).Include(x=>x.Candidates)
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (position.Interviews != null && position.Interviews.Any()) {
                    foreach (var c in position.Interviews.ToList())
                    {
                        _context.Interviews.Remove(c);
                    }
                }
                if (position.Candidates != null && position.Candidates.Any())
                {
                    foreach (var c in position.Candidates.ToList())
                    {
                        _context.Candidates.Remove(c);
                    }
                }

                _context.Positions.Remove(position);
                return await _context.SaveChangesAsync();
                
            }
            catch (Exception ex)
            {
                LogException(nameof(Delete), ex, $"Position deleted with ID: {id}");
                throw ex;
            }
        }

        public async Task<List<Position>> GetAll()
        {
            try
            {

                return await _context.Positions.Include(c => c.CarrerOffer).Include(c=>c.Interviews).Include(c=>c.Candidates).AsNoTracking().ToListAsync();

            }
            catch (Exception ex)
            {
                LogException(nameof(GetAll), ex,"Enable to get all the positions");
                throw ex;
            }
        }

        public async Task<Position> GetById(int id)
        {
            try
            {
                var postion = await _context.Positions
                    .Include(c => c.CarrerOffer).Include(c => c.Interviews).Include(c=>c.Candidates)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);
                return postion;
            }
            catch (Exception ex)
            {
                LogException(nameof(GetById), ex, $"Error retrieving position with ID: {id}");
                throw ex;
            }

        }

        public async Task<int> Insert(Position entity)
        {
            try
            {
                _context.Add(entity);
                return await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                LogException(nameof(Insert), ex, $"Position inserted with ID: {entity.Id}");
                throw ex;
            }
        }

        public async Task<int> Update(Position entity)
        {
            try
            {

                _context.Update(entity);
                return await _context.SaveChangesAsync();


            }
            catch (Exception ex)
            {
                LogException(nameof(Update), ex, $"Error updating position with ID: {entity.Id}");
                throw ex;
            }
        }


        public bool DoesPositionNameExist(string name)
        {
            try
            {
                return _context.Positions.Any(x => x.Name == name);
            }
            catch(Exception ex)
            {
                LogException(nameof(DoesPositionNameExist), ex, "DoesPositionNameExist not working ");
                throw ex;
            }
        }




        
    }
}

