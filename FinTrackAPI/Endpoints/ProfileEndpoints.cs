using FinTrackAPI.Data;
using FinTrackAPI.DTOs;
using FinTrackAPI.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Endpoints
{
    public static class ProfileEndpoints
    {
        public static void MapProfileEndpoints(this WebApplication app)
        {
            app.MapGet("/profile", async (HttpContext http, AppDbContext db) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                // Find the user in the database
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);

                if (user == null)
                    return Results.NotFound("User not found.");

                // Return the user's information
                return Results.Ok(new
                {
                    user.Username,
                    user.Email
                });
            })
            .RequireAuthorization();

            app.MapPut("/profile", async (HttpContext http, AppDbContext db, UpdateProfileRequest request) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);

                if (user == null)
                    return Results.NotFound("User not found.");

                // Email is not editable here on purpose - it's also the identity
                // key Google Sign-In matches on (see GoogleAuthEndpoints), so
                // changing it would silently repoint this account's data to
                // whoever signs in with the new email. Only the display name
                // can change.
                user.Username = request.Username;

                await db.SaveChangesAsync();

                return Results.Ok(new
                {
                    message = "Profile updated successfully."
                });
            })
            .RequireAuthorization();

            app.MapPut("/change-password", async (HttpContext http, AppDbContext db, ChangePasswordRequest request) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);

                if (user == null)
                    return Results.NotFound("User not found.");

                // Check current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    return Results.BadRequest("Current password is incorrect.");

                // Check if new passwords match
                if (request.NewPassword != request.ConfirmPassword)
                    return Results.BadRequest("New passwords do not match.");

                // Minimum password length
                if (request.NewPassword.Length < 6)
                    return Results.BadRequest("Password must be at least 6 characters.");

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                await db.SaveChangesAsync();

                return Results.Ok(new
                {
                    message = "Password changed successfully."
                });
            })
            .RequireAuthorization();
        }
    }
}