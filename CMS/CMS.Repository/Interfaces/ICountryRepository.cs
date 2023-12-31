﻿using CMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces
{
    public interface ICountryRepository
    {
        Task<int> Insert(Country entity);
        Task<int> Update(Country entity);
        int Delete(int id);
        //Task<int> Delete(int id);
        Task<Country> GetById(int id);
        Task<List<Country>> GetAll();

        bool DoesCountryNameExist(string name);
        Task<IEnumerable<Country>> GetAllCountriesAsync();

        void LogException(string methodName, Exception ex, string additionalInfo);
    }
}
