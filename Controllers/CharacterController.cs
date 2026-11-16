using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;

namespace MidnightSteepApi.Controllers
{
    public class CharacterController
    {
        private readonly ContextDb _ContextDb;
        public CharacterController(ContextDb context)
        {
            _ContextDb = context;
        }

        [HttpGet]
        [Route("characters")]
        public async Task<IActionResult> GetCharacters()
        {
            var characters = await _ContextDb.Characters.OrderBy(x => x.id).Select(x => new
                {
                    x.id,
                    x.name
                }).ToListAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    characters
                });
        }
    }
}
