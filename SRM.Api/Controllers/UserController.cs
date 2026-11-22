using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRM.Api.Models.Dto.User;
using SRM.Api.Models.Enums;
using SRM.Api.Models.Pagination;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils.Extensions;
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

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserListingDto>> GetUserById(string id)
        {
            if (!Guid.TryParse(id, out var userId))
                return BadRequest(new { Error = "El ID del usuario no es válido."});

            var response = await _userService.GetUserById(userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpGet("list")]
        public async Task<ActionResult<UserListingDto>> ListUsers([FromQuery] PaginationParameters filter)
        {
            if (!User.IsInRole(UserType.Admin.ToString()))
                return Forbid();

            var response = await _userService.ListUsers(filter);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("search")]
        public async Task<ActionResult<List<UserSearchDto>>> SearchUsers([FromQuery] PaginationParameters filter, string? query)
        {
            if (!User.IsInRole(UserType.Admin.ToString()))
                return Forbid();

            var response = await _userService.SearchUsersAsync(query, filter);
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
                Secure = true,
                SameSite = SameSiteMode.None,
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
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            #if DEBUG
                return Ok(new { Token = result.Value });
            #else
                return Ok();
            #endif
        }

        [Authorize]
        [HttpPut]
        public async Task<ActionResult<UserListingDto>> UpdateUser([FromBody] UpdateUserRequestDto request)
        {
            var userId = User.GetUserId();
            var response = await _userService.UpdateUser(userId.Value, request);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpDelete]
        public async Task<ActionResult> DeleteUser()
        {
            var userId = User.GetUserId();
            if (userId == null) return BadRequest(new { Error = "Id de usuario nulo." });

            if (User.IsInRole(UserType.Admin.ToString()))
                return BadRequest(new { Error = "Como administrador, no puede eliminar su cuenta." });

            var userCanDeleteAccount = await _userService.UserCanDeleteAccount(userId.Value);
            if (!userCanDeleteAccount.Success) return BadRequest(new { Error = userCanDeleteAccount.Error });

            await _userService.DeleteUser(userId.Value);
            return Ok(new { Success = true });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            //Response.Cookies.Delete("X-Access-Token");
            Response.Cookies.Delete("X-Access-Token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
            return Ok();
        }
    }
}
