using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlatformlarController : ControllerBase
    {
        private readonly IGenericRepository<PLATFORMLAR> _repo;

        public PlatformlarController(IGenericRepository<PLATFORMLAR> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IEnumerable<PLATFORMLAR>> GetAll()
        {
            return await _repo.GetAllAsync();
        }
    }
}
