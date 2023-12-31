﻿using CMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces
{
    public interface IPositionRepository
    {

        Task<int> Insert(Position entity);
        Task<int> Update(Position entity);
        Task<int> Delete(int id);
        Task<Position> GetById(int id);
        Task<List<Position>> GetAll();

        bool DoesPositionNameExist(string name);

        void LogException(string methodName, Exception ex, string additionalInfo);


    }
}
