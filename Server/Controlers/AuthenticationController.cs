using BaseLibrary.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Repositories.Contracts;

namespace Server.Controlers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController(IUserAccount account) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> CreateAsync(Register user)
        {
            if (user is null) return BadRequest("Model is empty");

            var result = await account.CreateAsync(user);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> SignUpAsync(Login user)
        {
            if (user is null) return BadRequest("Model is empty");

            var result = await account.SignUpAsync(user);
            return Ok(result);
        }
        
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokenAsync(RefreshToken token)
        {
            if (token is null) return BadRequest("Model is empty");

            var result = await account.RefreshTokenAsync(token);
            return Ok(result);
        }
    }
}
