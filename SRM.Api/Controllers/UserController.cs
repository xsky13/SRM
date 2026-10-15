using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto.User;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using System.Security.Claims;

namespace SRM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(IAuthService _authService, IUserService _userService) : ControllerBase
    {
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserListingDto>> GetUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = Guid.TryParse(userIdClaim, out var parsedUserId)
                ? parsedUserId
                : null;

            var response = await _userService.GetUser(parsedUserId);

            return response.ToActionResult();
        }

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
