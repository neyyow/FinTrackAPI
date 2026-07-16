using System.Security.Claims;

namespace FinTrackAPI.Extensions
{
    public static class HttpContextExtensions
    {
        public static int? GetUserId(this HttpContext http)
        {
            var claim = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(claim, out var userId) ? userId : null;
        }
    }
}
