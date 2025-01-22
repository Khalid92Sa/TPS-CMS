using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class TemplatesRepository : ITemplatesRepository
{
    private readonly ApplicationDbContext Db;

    public TemplatesRepository(ApplicationDbContext _db) => Db = _db;

    public async Task<IEnumerable<Templates>> GetAllTemplates() => await Db.Templates.ToListAsync();

    public async Task<Templates> GetTemplateById(int interviewId) => await Db.Templates.FindAsync(interviewId);

    public async Task Create(Templates entity)
    {
        entity.IsActive = true;
        entity.ModifiedBy = entity.ModifiedBy;
        entity.ModifiedOn = DateTime.Now;

        await Db.Templates.AddAsync(entity);
        await Db.SaveChangesAsync();
    }

    public async Task Update(Templates entity)
    {
        entity.IsActive = true;
        entity.ModifiedBy = entity.ModifiedBy;
        entity.ModifiedOn = DateTime.Now;

        Db.Templates.Update(entity);
        await Db.SaveChangesAsync();
    }

    public async Task Delete(Templates entity)
    {
        entity.IsDelete = true;
        entity.ModifiedBy = entity.ModifiedBy;
        entity.ModifiedOn = DateTime.Now;

        Db.Templates.Remove(entity);
        await Db.SaveChangesAsync();
    }
}