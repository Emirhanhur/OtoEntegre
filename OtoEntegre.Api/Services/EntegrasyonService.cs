using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;

namespace OtoEntegre.Api.Services
{
    public class EntegrasyonService
    {
        private readonly IGenericRepository<Entegrasyonlar> _repository;

        public EntegrasyonService(IGenericRepository<Entegrasyonlar> repository)
        {
            _repository = repository;
        }

        // CREATE

        
        // CREATE
        public async Task<Entegrasyonlar> CreateAsync(EntegrasyonCreateDto dto)
        {
            var entity = new Entegrasyonlar
            {
                Id = Guid.NewGuid(),
                Kullanici_Id = dto.Kullanici_Id,
                Platform_Id = dto.Platform_Id,
                Api_Key = dto.Api_Key,
                Api_Secret = dto.Api_Secret,
                Kullanici_Adi = dto.Kullanici_Adi,
                Seller_Id = dto.Seller_Id,
                Sifre = dto.Sifre,
                Extra_Config = dto.Extra_Config == null ? null : JsonSerializer.Serialize(dto.Extra_Config),
                Created_At = DateTime.UtcNow,  // Burada UTC kullan
                Updated_At = DateTime.UtcNow   // Burada da UTC kullan
            };

            await _repository.AddAsync(entity);
            await _repository.SaveAsync();
            return entity;
        }

        // UPDATE
        public async Task<Entegrasyonlar> UpdateAsync(Guid id, EntegrasyonCreateDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new KeyNotFoundException("Entegrasyon bulunamadı.");

            entity.Kullanici_Id = dto.Kullanici_Id;
            entity.Platform_Id = dto.Platform_Id;
            entity.Api_Key = dto.Api_Key;
            entity.Api_Secret = dto.Api_Secret;
            entity.Kullanici_Adi = dto.Kullanici_Adi;
            entity.Sifre = dto.Sifre;
            entity.Extra_Config = dto.Extra_Config == null ? null : JsonSerializer.Serialize(dto.Extra_Config);
            entity.Updated_At = DateTime.UtcNow; // UTC yap

            await _repository.SaveAsync();
            return entity;
        }


        // GET BY ID
        public async Task<Entegrasyonlar?> GetByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // GET ALL
        public async Task<IEnumerable<Entegrasyonlar>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        // DELETE
        // DELETE
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new KeyNotFoundException("Entegrasyon bulunamadı.");

            _repository.Delete(entity);  // burada CollectionExtensions.Remove değil Delete kullanılacak
            await _repository.SaveAsync();
        }



    }
}
