using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace OtoEntegre.Api.Services
{
    public class UserService
    {
        private readonly IGenericRepository<KULLANICILAR> _userRepository;
        private readonly IGenericRepository<KullaniciRolleri> _userRoleRepository;
        private readonly IGenericRepository<ROLLER> _rolRepository;
        private readonly IConfiguration _configuration;

        public UserService(
            IGenericRepository<KULLANICILAR> userRepository,
    IGenericRepository<ROLLER> rolRepository,
    IGenericRepository<KullaniciRolleri> userRoleRepository,
    IConfiguration configuration)
        {
            _userRepository = userRepository;
            _rolRepository = rolRepository;
            _userRoleRepository = userRoleRepository;
            _configuration = configuration;
        }
        // Login metodu
        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = (await _userRepository.GetAllAsync())
                .FirstOrDefault(u => u.Email == dto.Email);

            if (user == null)
                return null;

            if (!VerifyPassword(dto.Password, user.Sifre_Hash))
                return null;

            var token = await GenerateJwtTokenAsync(user);
            var userDto = MapToDto(user);

            return new LoginResponseDto
            {
                Token = token,
                User = userDto
            };
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            // BCrypt ile hash kontrolü
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        private async Task<string> GenerateJwtTokenAsync(KULLANICILAR user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("Jwt:Key configuration missing!");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Kullanıcı rolleri asenkron şekilde alınıyor
            var allUserRoles = await _userRoleRepository.GetAllAsync();
            var userRoles = new List<string>();

            foreach (var ur in allUserRoles.Where(r => r.KullaniciId == user.Id))
            {
                var rol = await _rolRepository.GetByIdAsync(ur.RolId);
                if (rol != null)
                    userRoles.Add(rol.Ad);
                else
                    userRoles.Add("RolBulunamadı");
            }

            var claims = new List<Claim>
    {
        new Claim("nameid", user.Id.ToString()),
        new Claim("name", user.Ad ?? string.Empty),
        new Claim("email", user.Email ?? string.Empty)
    };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim("role", role));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Tüm kullanıcıları getir
        public async Task<IEnumerable<KullaniciDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<KullaniciDto?> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user != null ? MapToDto(user) : null;
        }

        // Kullanıcı oluştur
        public async Task<KullaniciDto> CreateAsync(CreateKullaniciDto dto)
        {
            var rol = await _rolRepository.GetByIdAsync(dto.RolId);
            if (rol == null)
                throw new Exception("Geçersiz RolId. Lütfen var olan bir rol seçin.");

            var useSame = dto.TelegramUseSamePhone == true;
            var entegrasyonPhone = useSame ? dto.Telefon : (dto.Entegrasyon_Telefon ?? string.Empty);

            var newUser = new KULLANICILAR
            {
                Id = Guid.NewGuid(),
                Ad = dto.Ad,
                Email = dto.Email,
                Telefon = useSame ? entegrasyonPhone : dto.Telefon,
                Entegrasyon_Telefon = entegrasyonPhone,
                Sifre_Hash = HashPassword(dto.Sifre),
                Tedarik_Kullanici_Id = dto.Tedarik_Kullanici_Id,
                Created_At = DateTime.UtcNow,
                Updated_At = DateTime.UtcNow,
                Telegram_Chat = dto.Telegram_Chat,
                Telegram_Token = dto.Telegram_Token,

            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveAsync();

            var userRole = new KullaniciRolleri
            {
                Id = Guid.NewGuid(),
                KullaniciId = newUser.Id,
                RolId = dto.RolId
            };

            await _userRoleRepository.AddAsync(userRole);
            await _userRoleRepository.SaveAsync();

            return MapToDto(newUser);
        }

        public async Task UpdateAsync(Guid id, UpdateKullaniciDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new Exception("Kullanıcı bulunamadı");

            var useSame = dto.TelegramUseSamePhone == true;
            var entegrasyonPhone = useSame ? dto.Telefon : (dto.Entegrasyon_Telefon ?? user.Entegrasyon_Telefon ?? user.Telefon);

            user.Ad = dto.Ad;
            user.Email = dto.Email;
            user.Telefon = useSame ? entegrasyonPhone : dto.Telefon;
            user.Entegrasyon_Telefon = entegrasyonPhone;
            user.Updated_At = DateTime.UtcNow;
           user.Telegram_Chat = dto.Telegram_Chat ?? user.Telegram_Chat;
user.Telegram_Token = dto.Telegram_Token ?? user.Telegram_Token;


            _userRepository.Update(user);
            await _userRepository.SaveAsync();

            var userRole = (await _userRoleRepository.GetAllAsync())
                .FirstOrDefault(r => r.KullaniciId == user.Id);

            // Rol güncellemesini sadece geçerli bir RolId gönderildiyse yap
            if (dto.RolId != Guid.Empty)
            {
                var existingRole = await _rolRepository.GetByIdAsync(dto.RolId);
                if (existingRole != null && userRole != null)
                {
                    userRole.RolId = dto.RolId;
                    _userRoleRepository.Update(userRole);
                    await _userRoleRepository.SaveAsync();
                }
                // Geçersiz rol gönderildiyse rol güncellemesini atla (kullanıcı alanları zaten kaydedildi)
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null)
            {
                _userRepository.Delete(user);
                await _userRepository.SaveAsync();

                var userRole = (await _userRoleRepository.GetAllAsync())
                    .FirstOrDefault(r => r.KullaniciId == user.Id);

                if (userRole != null)
                {
                    _userRoleRepository.Delete(userRole);
                    await _userRoleRepository.SaveAsync();
                }
            }
        }

        private string HashPassword(string password)
        {
            // BCrypt ile hashle
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.Sifre_Hash))
                return false;

            user.Sifre_Hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.Updated_At = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveAsync();
            return true;
        }

        private KullaniciDto MapToDto(KULLANICILAR user)
        {
            return new KullaniciDto
            {
                Id = user.Id,
                Ad = user.Ad,
                Email = user.Email,
                Telegram_Chat = user.Telegram_Chat,
                Telegram_Token = user.Telegram_Token,
                Telefon = user.Telefon,
                Entegrasyon_Telefon = user.Entegrasyon_Telefon,
                Tedarik_Kullanici_Id = user.Tedarik_Kullanici_Id,
                Created_At = user.Created_At,
                Updated_At = user.Updated_At,
                Roller = user.Roller.Select(ur => new KullaniciRolDto
                {
                    Id = ur.Id,
                    KullaniciId = ur.KullaniciId,
                    RolId = ur.RolId,
                    Rol = ur.Rol != null ? new RolDto
                    {
                        Id = ur.Rol.Id,
                        Ad = ur.Rol.Ad,
                        Aciklama = ur.Rol.Aciklama
                    } : null!
                }).ToList()
            };
        }
    }
}




