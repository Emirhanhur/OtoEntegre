using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using OtoEntegre.Api.Services;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KredilerController : ControllerBase
    {
        private readonly KredilerService _kredilerService;

        public KredilerController(KredilerService kredilerService)
        {
            _kredilerService = kredilerService;
        }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _kredilerService.GetAllUserCreditsAsync();
        return Ok(list);
    }

    [HttpGet("{kullaniciId}")]
    public async Task<IActionResult> Get(Guid kullaniciId)
    {
        var kred = await _kredilerService.GetByKullaniciAsync(kullaniciId);
        if (kred == null) return NotFound();
        return Ok(new { kred.KullaniciId, kred.KalanKredi, kred.SonSatinAlim });
    }

    [HttpPost("{kullaniciId}/add")]
    public async Task<IActionResult> Add(Guid kullaniciId, [FromQuery] int amount)
    {
        if (amount <= 0) return BadRequest(new { success = false, error = "Invalid amount" });
        await _kredilerService.AddCreditsAsync(kullaniciId, amount);
        return Ok(new { success = true });
    }

    [HttpPost("{kullaniciId}/consume")]
    public async Task<IActionResult> Consume(Guid kullaniciId)
    {
        var ok = await _kredilerService.ConsumeOneAsync(kullaniciId);
        if (!ok) return BadRequest(new { success = false, error = "Krediniz kalmadı. Lütfen kredi yükleyiniz." });
        return Ok(new { success = true });
    }
    }
}
