using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OtoEntegre.Api.Entities;
namespace OtoEntegre.Api.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task AddAsync(T entity);
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
            Task AddRangeAsync(IEnumerable<T> entities); // ✅ Bunu ekle

        void Update(T entity);
        void Delete(T entity);
        Task SaveAsync();
        
        
    }
}
