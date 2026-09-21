using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto.User;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(IAuthService _authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<string>> RegisterUser([FromBody] CreateUserRequestDto request)
        {
            // retorna el token
            var result = await _authService.RegisterUser(request.FirstName, request.LastName, request.Telefono, request.Email, request.Pwd);
            if (!result.Success) return BadRequest(new { Error = result.Error });

            Response.Cookies.Append("X-Access-Token", result.Value!, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

        #if DEBUG
            return Ok(new { Token = result.Value });
        #else
            return Ok();
        #endif
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> LoginUser([FromBody] LoginUserRequestDto request)
        {
            // retorna el token
            var result = await _authService.LoginUser(request.Email, request.Pwd);
            if (!result.Success) return BadRequest(new { Error = result.Error });

            Response.Cookies.Append("X-Access-Token", result.Value!, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            #if DEBUG
                return Ok(new { Token = result.Value });
            #else
                return Ok();
            #endif
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            Response.Cookies.Delete("X-Access-Token");
            return Ok();
        }
    }
}
