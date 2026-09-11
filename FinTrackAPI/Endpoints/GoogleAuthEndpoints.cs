using FinTrackAPI.Data;
using FinTrackAPI.DTOs;
using FinTrackAPI.Models;
using FinTrackAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FinTrackAPI.Endpoints
{
    // The shape of Google's response from the tokeninfo endpoint.
    // Only the fields we actually need are listed here.
    public class GoogleTokenInfo
    {
        [JsonPropertyName("aud")]
        public string Aud { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        // Google sends this as the literal string "true"/"false", not a real boolean.
        [JsonPropertyName("email_verified")]
        public string EmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public static class GoogleAuthEndpoints
    {
        public static void MapGoogleAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/auth/google", async (
                AppDbContext db,
                JwtService jwt,
                IHttpClientFactory httpClientFactory,
                IConfiguration config,
                GoogleLoginRequest request) =>
            {
                if (string.IsNullOrWhiteSpace(request.IdToken))
                    return Results.BadRequest("Missing Google sign-in token.");

                // Ask Google itself to verify this token - checks the signature and expiry for us.
                // This is an outbound call from the server, so it can fail for reasons that have
                // nothing to do with the sign-in itself (host network restrictions, a timeout,
                // Google being briefly unreachable). Without this try/catch, any of those throw
                // an unhandled exception, which strips the CORS header from the response and
                // shows up in the browser as a misleading "blocked by CORS policy" error instead
                // of the real problem.
                GoogleTokenInfo? tokenInfo;
                try
                {
                    var client = httpClientFactory.CreateClient();
                    var response = await client.GetAsync(
                        $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(request.IdToken)}");

                    if (!response.IsSuccessStatusCode)
                        return Results.BadRequest("Could not verify Google sign-in. Please try again.");

                    tokenInfo = await response.Content.ReadFromJsonAsync<GoogleTokenInfo>();
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: $"Could not reach Google to verify sign-in: {ex.Message}",
                        statusCode: StatusCodes.Status502BadGateway);
                }

                var expectedClientId = config["Google:ClientId"];
                if (tokenInfo == null || string.IsNullOrEmpty(expectedClientId) || tokenInfo.Aud != expectedClientId)
                    return Results.BadRequest("This Google sign-in wasn't issued for this app.");

                if (tokenInfo.EmailVerified != "true")
                    return Results.BadRequest("Your Google email address isn't verified.");

                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == tokenInfo.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Username = string.IsNullOrWhiteSpace(tokenInfo.Name)
                            ? tokenInfo.Email.Split('@')[0]
                            : tokenInfo.Name,
                        Email = tokenInfo.Email,
                        // This account has no usable password - it can only ever sign in
                        // via Google. A random hash here just satisfies the non-null column.
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                }

                var token = jwt.GenerateToken(user.Id, user.Email);

                return Results.Ok(new
                {
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email
                });
            });
        }
    }
}
