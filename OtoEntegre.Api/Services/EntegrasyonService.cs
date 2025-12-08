using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using OtoEntegre.Api.Data;

namespace OtoEntegre.Api.Services
{
    public class EntegrasyonService
    {
        private readonly IGenericRepository<Entegrasyonlar> _repository;
        private readonly AppDbContext _db;

        public EntegrasyonService(IGenericRepository<Entegrasyonlar> repository, AppDbContext db)
        {
            _repository = repository;
            _db = db;
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
            // Only update Api_Secret when a non-empty value is provided to avoid wiping existing secret
            if (!string.IsNullOrWhiteSpace(dto.Api_Secret))
            {
                entity.Api_Secret = dto.Api_Secret;
            }
            entity.Kullanici_Adi = dto.Kullanici_Adi;
            entity.Sifre = dto.Sifre;
            entity.Seller_Id = dto.Seller_Id; // ensure seller id is updated
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

        public async Task<IEnumerable<OtoEntegre.Api.DTOs.EntegrasyonUserDto>> GetUsersWithIntegrationsAsync()
        {
            var query = from u in _db.Kullanicilar
                        join e in _db.Entegrasyonlar on u.Id equals e.Kullanici_Id into g
                        from ent in g.DefaultIfEmpty()
                        select new OtoEntegre.Api.DTOs.EntegrasyonUserDto
                        {
                            KullaniciId = u.Id,
                            Ad = u.Ad,
                            Email = u.Email,
                            Telefon = u.Telefon,
                            Entegrasyon = ent == null ? null : new OtoEntegre.Api.DTOs.IntegrationDto
                            {
                                Id = ent.Id,
                                ApiKey = ent.Api_Key ?? string.Empty,
                                ApiSecret = ent.Api_Secret ?? string.Empty,
                                SellerId = ent.Seller_Id,
                                PlatformId = ent.Platform_Id,
                                PlatformAdi = ent.Platform != null ? ent.Platform.Ad : string.Empty
                            }
                        };

            return await query.ToListAsync();
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
