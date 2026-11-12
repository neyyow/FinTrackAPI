using FinTrackAPI.Data;
using FinTrackAPI.DTOs;
using FinTrackAPI.Extensions;
using FinTrackAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Endpoints
{
    public static class WalletEndpoints
    {
        private const int MaxNameLength = 50;

        // Returns an error message if the name is not acceptable, otherwise null.
        // excludeId lets a wallet keep its own name when it is being renamed.
        private static async Task<string?> ValidateName(AppDbContext db, int userId, string? name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Wallet name is required.";

            if (name.Length > MaxNameLength)
                return $"Wallet name must be {MaxNameLength} characters or fewer.";

            var lowered = name.ToLower();
            var duplicate = await db.Wallets.AnyAsync(w =>
                w.UserId == userId && w.Id != excludeId && w.Name.ToLower() == lowered);

            return duplicate ? $"You already have a wallet named \"{name}\"." : null;
        }

        public static void MapWalletEndpoints(this WebApplication app)
        {
            // GET ALL - only the signed-in user's wallets, with how many expenses use each
            app.MapGet("/wallet", async (AppDbContext db, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var wallets = await db.Wallets
                    .Where(w => w.UserId == userId.Value)
                    .OrderBy(w => w.Name)
                    .Select(w => new
                    {
                        w.Id,
                        w.Name,
                        ExpenseCount = db.Expenses.Count(e => e.WalletId == w.Id && e.UserId == userId.Value)
                    })
                    .ToListAsync();

                return Results.Ok(wallets);
            }).RequireAuthorization();

            // CREATE
            app.MapPost("/wallet", async (AppDbContext db, WalletRequest request, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var name = request.Name?.Trim();
                var error = await ValidateName(db, userId.Value, name);
                if (error != null) return Results.BadRequest(error);

                var wallet = new Wallet { UserId = userId.Value, Name = name! };
                db.Wallets.Add(wallet);
                await db.SaveChangesAsync();

                return Results.Ok(new { wallet.Id, wallet.Name, ExpenseCount = 0 });
            }).RequireAuthorization();

            // RENAME - only if it belongs to the signed-in user
            app.MapPut("/wallet/{id}", async (AppDbContext db, int id, WalletRequest request, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var wallet = await db.Wallets
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId.Value);

                if (wallet == null) return Results.NotFound();

                var name = request.Name?.Trim();
                var error = await ValidateName(db, userId.Value, name, excludeId: id);
                if (error != null) return Results.BadRequest(error);

                wallet.Name = name!;
                await db.SaveChangesAsync();

                var expenseCount = await db.Expenses
                    .CountAsync(e => e.WalletId == id && e.UserId == userId.Value);

                return Results.Ok(new { wallet.Id, wallet.Name, ExpenseCount = expenseCount });
            }).RequireAuthorization();

            // DELETE - only if it belongs to the signed-in user.
            // The expenses stay; they just stop pointing at this wallet.
            app.MapDelete("/wallet/{id}", async (AppDbContext db, int id, HttpContext http) =>
            {
                var userId = http.GetUserId();
                if (userId is null) return Results.Unauthorized();

                var wallet = await db.Wallets
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId.Value);

                if (wallet == null) return Results.NotFound();

                var linkedExpenses = await db.Expenses
                    .Where(e => e.WalletId == id && e.UserId == userId.Value)
                    .ToListAsync();

                foreach (var expense in linkedExpenses)
                    expense.WalletId = null;

                db.Wallets.Remove(wallet);
                await db.SaveChangesAsync(); // one save = all-or-nothing

                return Results.Ok("Deleted successfully");
            }).RequireAuthorization();
        }
    }
}
