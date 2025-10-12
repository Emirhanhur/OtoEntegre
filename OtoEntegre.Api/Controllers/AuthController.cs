using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Services;
using OtoEntegre.Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var loginResponse = await _userService.LoginAsync(dto);
                if (loginResponse == null)
                    return Unauthorized(new { message = "Email veya şifre yanlış!" });

                return Ok(loginResponse);
            }
            catch (Exception ex)
            {
                // Hata logla
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpGet("admin-data")]
        public IActionResult GetAdminData()
        {
            return Ok(new { message = "Sadece admin görebilir" });
        }

    }
}
