using System.Security.Claims;

namespace SRM.Api.Utils
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userId, out var userGuid))
                return userGuid;

            return null;
        }
    }
}
