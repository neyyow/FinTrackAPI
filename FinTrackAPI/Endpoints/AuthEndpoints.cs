using FinTrackAPI.Data;
using FinTrackAPI.Models;
using FinTrackAPI.DTOs;
using FinTrackAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            // REGISTER
            app.MapPost("/register", async (AppDbContext db, RegisterRequest request) =>
            {
                var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                    return Results.BadRequest("User already exists");

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                return Results.Ok("User registered successfully");
            });
            // LOGIN
            app.MapPost("/login", async (AppDbContext db, JwtService jwt, LoginRequest request) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                    return Results.BadRequest("This email doesn't exist. Check the spelling, or create an account.");

                var isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

                if (!isValidPassword)
                    return Results.BadRequest("Incorrect password. Please try again.");

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