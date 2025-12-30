using CMS.Domain.Entities;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces;

public interface ISelectedInterviewersRepository
{
    Task<SelectedInterviewers> GetByInterviewIdAsync(int interviewId);
    Task<int> InsertAsync(SelectedInterviewers entity);
    Task UpdateAsync(SelectedInterviewers entity);
    Task DeleteAsync(int id);
}

