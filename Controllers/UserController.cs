using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidnightSteepApi.Data;
using MidnightSteepApi.Models;

namespace MidnightSteepApi.Controllers
{
    public class UserController
    {
        private readonly ContextDb _ContextDb;
        public UserController(ContextDb context)
        {
            _ContextDb = context;
        }

        [HttpPost]
        [Route("registrationUser")]
        public async Task<IActionResult> RegistrationUser([FromBody] User regUser)
        {
            var isUsernameNotUQ = await _ContextDb.Users.AnyAsync(x => x.login == regUser.login);
            if (isUsernameNotUQ)
            {
                return new OkObjectResult(new
                    {
                        status = false,
                        error = "пользователь уже существует"
                    });
            }

            var user = new User()
            {
                login = regUser.login,
                password = regUser.password,
                last_login = DateTime.UtcNow
            };
            await _ContextDb.AddAsync(user);
            await _ContextDb.SaveChangesAsync();
            return new OkObjectResult(new
                {
                    status = true,
                    user
                });
        }

        [HttpPost]
        [Route("authUser")]
        public async Task<IActionResult> AuthUser([FromBody] User loginUser)
        {
            var user = await _ContextDb.Users.FirstOrDefaultAsync(x => x.login == loginUser.login && x.password == loginUser.password);
            if (user == null)
            {
                return new OkObjectResult(new
                    {
                        status = false,
                        error = "неверный логин или пароль"
                    });
            }

            return new OkObjectResult(new
                {
                    status = true,
                    user
                });
        }
    }
}
