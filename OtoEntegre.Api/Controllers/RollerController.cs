using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using OtoEntegre.Api.DTOs;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RollerController : ControllerBase
    {
        private readonly IGenericRepository<ROLLER> _repo;

        public RollerController(IGenericRepository<ROLLER> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IEnumerable<ROLLER>> GetAll()
            => await _repo.GetAllAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<ROLLER>> GetById(Guid id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }



        [HttpPost]
public async Task<ActionResult<ROLLER>> Create(RolCreateDto dto)
{
    var rol = new ROLLER
    {
        Id = Guid.NewGuid(),
        Ad = dto.Ad,
        Aciklama = dto.Aciklama
    };

    await _repo.AddAsync(rol);
    await _repo.SaveAsync();
    return CreatedAtAction(nameof(GetById), new { id = rol.Id }, rol);
}


        [HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, RolUpdateDto dto)
{
    var rol = await _repo.GetByIdAsync(id);
    if (rol == null) return NotFound();

    rol.Ad = dto.Ad;
    rol.Aciklama = dto.Aciklama;

    _repo.Update(rol);
    await _repo.SaveAsync();

    return NoContent();
}


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();

            _repo.Delete(item);
            await _repo.SaveAsync();
            return NoContent();
        }
    }
}
