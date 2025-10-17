using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Services;
using OtoEntegre.Api.DTOs;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IEnumerable<KullaniciDto>> GetAll()
            => await _userService.GetAllAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<KullaniciDto>> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<KullaniciDto>> Create(CreateKullaniciDto dto)
        {
            var createdUser = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateKullaniciDto dto)
        {
            await _userService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _userService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(Guid id, ChangePasswordDto dto)
        {
            if (dto.OldPassword == null || dto.NewPassword == null || dto.ConfirmPassword == null)
                return BadRequest(new { message = "Tüm alanlar doldurulmalıdır." });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Yeni şifreler eşleşmiyor." });

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            var success = await _userService.ChangePasswordAsync(id, dto.OldPassword, dto.NewPassword);
            if (!success)
                return BadRequest(new { message = "Eski şifre yanlış." });

            return Ok(new { message = "Şifre başarıyla değiştirildi." });
        }

        public class ChangePasswordDto
        {
            public string? OldPassword { get; set; }
            public string? NewPassword { get; set; }
            public string? ConfirmPassword { get; set; }
        }

    }
}
